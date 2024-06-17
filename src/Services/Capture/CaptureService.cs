using Chronofoil.CaptureFile;
using Chronofoil.CaptureFile.Generated;
using Chronofoil.Common.Capture;
using Chronofoil.Web.Persistence;
using Chronofoil.Web.Services.Database;
using Microsoft.AspNetCore.Mvc;
using OneOf;

namespace Chronofoil.Web.Services.Capture;

public class CaptureService
{
    private readonly ILogger<CaptureService> _logger;
    private readonly IServiceProvider _services;
    
    private readonly string _uploadDirectory; 
    
    public CaptureService(ILogger<CaptureService> logger, IConfiguration config, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
        _uploadDirectory = config["UploadDirectory"]!;
        if (!Directory.Exists(_uploadDirectory))
            Directory.CreateDirectory(_uploadDirectory);
    }

    private bool ValidateCapture(string capturePath, CaptureUploadRequest request, out CaptureInfo? captureInfo)
    {
        captureInfo = null;
        
        var detectedIdString = Path.GetFileNameWithoutExtension(capturePath);
        if (!Guid.TryParse(detectedIdString, out var detectedId))
        {
            _logger.LogError("Failed to parse provided filename into a Guid.");
            return false;
        }
        
        var captureReader = new CaptureReader(capturePath);
        var start = captureReader.CaptureInfo.CaptureStartTime.ToDateTime();
        var end = captureReader.CaptureInfo.CaptureEndTime.ToDateTime();
        
        _logger.LogInformation("Capture start: {startTime} Capture end: {endTime}", start, end);

        var captureIdString = captureReader.CaptureInfo.CaptureId;
        if (!Guid.TryParse(captureIdString, out var captureId))
        {
            _logger.LogError("Failed to parse captureId from capture into a Guid.");
            return false;
        }

        if (detectedId != captureId || captureId != request.CaptureId)
        {
            _logger.LogError("Detected ID did not match provided ID from either the file or the request.");
            return false;   
        }

        if (end == DateTime.UnixEpoch.ToUniversalTime())
        {
            _logger.LogError("Capture end time was Unix Epoch (unset)");
            return false;
        }

        // The validation is 7 and 14 days, but this is easier to validate as the time starts ticking when the
        // user first visits the upload modal.
        if (!request.MetricWhenEos && request.MetricTime < DateTime.UtcNow + TimeSpan.FromDays(6))
        {
            _logger.LogError("Metric time was too soon.");
            return false;
        }
        
        if (!request.PublicWhenEos && request.PublicTime < DateTime.UtcNow + TimeSpan.FromDays(13))
        {
            _logger.LogError("Public time was too soon.");
            return false;
        }
        
        if (start >= end)
        {
            // if (DateTime.IsLeapYear(start.Year)
            //     && start.Month is 2
            //     && start.Day is 28 or 29)
            // {
            //     _logger.LogInformation($"lol leap year");
            // }
            // else
            {
                _logger.LogError("Capture start time was after or equal to end time?");
                return false;   
            }
        }
        
        try
        {
            var list = captureReader.GetFrames().ToList();
            GC.KeepAlive(list); // don't optimize away, but don't do anything with the frames
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to read a frame in capture {captureId}", captureReader.CaptureInfo.CaptureId);
            return false;
        }

        captureInfo = captureReader.CaptureInfo;
        return true;
    }

    public async Task<OneOf<CaptureUploadResponse, StatusCodeResult>> 
        Upload(Guid userId, CaptureUploadRequest request, IFormFile file)
    {
        var scopeFactory = _services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CfDbService>();
        
        var captureId = request.CaptureId;
        if (await db.GetUploadById(captureId) != null)
            return new StatusCodeResult(StatusCodes.Status409Conflict);

        var outPath = Path.Combine(_uploadDirectory, $"{captureId}.ccfcap");
        if (File.Exists(outPath))
        {
            _logger.LogError("Got past DB upload check but file already exists?");
            return new StatusCodeResult(StatusCodes.Status409Conflict);
        }

        {
            await using var stream = File.OpenWrite(outPath);
            await file.OpenReadStream().CopyToAsync(stream);   
        }

        if (!ValidateCapture(outPath, request, out var captureInfo) || captureInfo == null)
        {
            _logger.LogError("Capture {captureId} failed validation", request.CaptureId);
            return new StatusCodeResult(StatusCodes.Status400BadRequest);
        }
        
        var metricTime = request.MetricTime.ToUniversalTime();
        var publicTime = request.PublicTime.ToUniversalTime();
        var dbUpload = new ChronofoilUpload(
            request.CaptureId,
            userId,
            captureInfo.CaptureStartTime.ToDateTime(),
            captureInfo.CaptureEndTime.ToDateTime(),
            metricTime,
            request.MetricWhenEos,
            publicTime,
            request.PublicWhenEos);
        await db.AddUpload(dbUpload);
        await db.Save();
        
        return new CaptureUploadResponse
        {
            CaptureId = dbUpload.CfCaptureId,
            MetricTime = dbUpload.MetricTime,
            MetricWhenEos = dbUpload.MetricWhenEos,
            PublicTime = dbUpload.PublicTime,
            PublicWhenEos = dbUpload.PublicWhenEos
        };
    }
    
    public async Task<StatusCodeResult> Delete(Guid userId, Guid captureId)
    {
        _logger.LogInformation("User {user} removing capture {capture}", userId, captureId);
        
        var scopeFactory = _services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CfDbService>();

        var upload = await db.GetUploadById(captureId);
        if (upload == null) return new NotFoundResult();
        if (upload.UserId != userId)
        {
            _logger.LogError("User {user} is not the uploader of {capture}", userId, captureId);
            return new UnauthorizedResult();
        }
        db.RemoveUpload(upload);

        var uploadFile = Path.Combine(_uploadDirectory, $"{captureId}.ccfcap");
        if (!File.Exists(uploadFile))
        {
            _logger.LogError("Tried to delete capture {guid} but couldn't find file at expected location {dir}", captureId, uploadFile);
        }
        else
        {
            File.Delete(uploadFile);   
        }
        
        await db.Save();
        
        return new OkResult();
    }
    
    public async Task<CaptureListResponse> GetCaptures(Guid userId)
    {
        _logger.LogInformation("User {user} requesting capture list", userId);
        
        var scopeFactory = _services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CfDbService>();

        var uploads = await db.GetUploads(userId);
        var elements = uploads.Select(upload => new CaptureListElement
        {
            CaptureId = upload.CfCaptureId,
            StartTime = upload.StartTime,
            EndTime = upload.EndTime,
            MetricsTime = upload.MetricTime,
            PublicTime = upload.PublicTime,
            MetricsWhenEos = upload.MetricWhenEos,
            PublicWhenEos = upload.PublicWhenEos
        }).ToList();
        
        _logger.LogInformation("Returning {count} captures", elements.Count);
        
        return new CaptureListResponse { Captures = elements };
    }
}