using System.Globalization;
using Chronofoil.CaptureFile;
using Chronofoil.CaptureFile.Generated;
using Chronofoil.Common;
using Chronofoil.Common.Capture;
using Chronofoil.Web.Persistence;
using Chronofoil.Web.Services.Database;
using Chronofoil.Web.Services.Storage;

namespace Chronofoil.Web.Services.Capture;

public class CaptureService : ICaptureService
{
    private readonly ILogger<CaptureService> _logger;
    private readonly IDbService _db;
    private readonly IStorageService _storageService;
    
    public CaptureService(ILogger<CaptureService> logger, IDbService db, IStorageService storageService)
    {
        _logger = logger;
        _db = db;
        _storageService = storageService;
    }

    private bool ValidateCapture(
        string capturePath,
        CaptureUploadRequest request,
        out VersionInfo? versionInfo, 
        out CaptureInfo? captureInfo)
    {
        versionInfo = null;
        captureInfo = null;
        
        var detectedIdString = Path.GetFileNameWithoutExtension(capturePath);
        if (!Guid.TryParse(detectedIdString, out var detectedId))
        {
            _logger.LogError("Failed to parse provided filename into a Guid.");
            return false;
        }
        
        using var captureReader = new CaptureReader(capturePath);
        var start = captureReader.CaptureInfo.CaptureStartTime.ToDateTime();
        var end = captureReader.CaptureInfo.CaptureEndTime.ToDateTime();
        
        _logger.LogInformation("Capture start: {startTime} Capture end: {endTime}", start, end);

        var captureIdString = captureReader.CaptureInfo.CaptureId;
        if (!Guid.TryParse(captureIdString, out var captureId))
        {
            _logger.LogError("Failed to parse captureId from capture into a Guid.");
            return false;
        }

        if (detectedId != captureId || captureId != request.CaptureId || detectedId != request.CaptureId)
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

        versionInfo = captureReader.VersionInfo;
        captureInfo = captureReader.CaptureInfo;
        return true;
    }

    public async Task<ApiResult<CaptureUploadResponse>> 
        Upload(Guid userId, CaptureUploadRequest request, IFormFile file)
    {
        var captureId = request.CaptureId;
        if (await _db.GetUploadById(captureId) != null)
            return ApiResult<CaptureUploadResponse>.Failure(ApiStatusCode.CaptureExists);

        var fileMeta = await _storageService.FileExistsAsync($"{captureId}.ccfcap"); 
        if (fileMeta != null)
            return ApiResult<CaptureUploadResponse>.Failure(ApiStatusCode.CaptureExists);

        var fileName = $"{captureId}.ccfcap";

        var tempDir = Directory.CreateTempSubdirectory();
        var tempPath = Path.Combine(tempDir.FullName, fileName);
        try
        {
            await using (var tempStream = File.OpenWrite(tempPath))
            {
                await file.OpenReadStream().CopyToAsync(tempStream);
            }

            var isValid = ValidateCapture(tempPath, request, out var versionInfo, out var captureInfo); 
            
            if (!isValid || versionInfo == null || captureInfo == null)
            {
                _logger.LogError("Capture {captureId} failed validation", request.CaptureId);
                return ApiResult<CaptureUploadResponse>.Failure(ApiStatusCode.CaptureNotValid);
            }
            
            var metadata = GetMetadata(request, versionInfo, captureInfo);

            await using var uploadStream = File.OpenRead(tempPath);
            var uploadResult = await _storageService.UploadFileAsync(fileName, uploadStream, metadata);
            if (!uploadResult)
            {
                _logger.LogError("Failed to upload capture {captureId}", request.CaptureId);
                return ApiResult<CaptureUploadResponse>.Failure(ApiStatusCode.CaptureUploadFailed);
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
            await _db.AddUpload(dbUpload);
            await _db.Save();

            return ApiResult<CaptureUploadResponse>.Success(new CaptureUploadResponse
            {
                CaptureId = dbUpload.CfCaptureId,
                MetricTime = dbUpload.MetricTime,
                MetricWhenEos = dbUpload.MetricWhenEos,
                PublicTime = dbUpload.PublicTime,
                PublicWhenEos = dbUpload.PublicWhenEos
            });
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    private Dictionary<string, string> GetMetadata(
        CaptureUploadRequest request, 
        VersionInfo versionInfo,
        CaptureInfo captureInfo)
    {
        return new Dictionary<string, string>
        {
            ["capture_id"] = captureInfo.CaptureId,
            ["capture_start_time"] = captureInfo.CaptureStartTime.ToString(),
            ["capture_end_time"] = captureInfo.CaptureStartTime.ToString(),
            ["metric_time"] = request.MetricTime.ToString(CultureInfo.InvariantCulture),
            ["metric_when_eos"] = request.MetricWhenEos.ToString(),
            ["public_time"] = request.PublicTime.ToString(CultureInfo.InvariantCulture),
            ["public_when_eos"] = request.PublicWhenEos.ToString(),
            ["game_version"] = versionInfo.GameVer[0]
        };
    }
    
    public async Task<ApiResult> Delete(Guid userId, Guid captureId)
    {
        _logger.LogInformation("User {user} removing capture {capture}", userId, captureId);
        
        var upload = await _db.GetUploadById(captureId);
        if (upload == null) 
            return ApiResult.Failure(ApiStatusCode.CaptureNotFound);
        if (upload.UserId != userId)
        {
            _logger.LogError("User {user} is not the uploader of {capture}", userId, captureId);
            return ApiResult.Failure(ApiStatusCode.CaptureNotFoundForUser);
        }
        _db.RemoveUpload(upload);

        var fileName = $"{captureId}.ccfcap";
        var deleteResult = await _storageService.DeleteFileAsync(fileName);
        if (!deleteResult)
        {
            _logger.LogWarning("Failed to delete file {fileName} from S3, but continuing with database cleanup", fileName);
        }
        
        await _db.Save();
        
        return ApiResult.Success();
    }
    
    public async Task<ApiResult<CaptureListResponse>> GetCaptures(Guid userId)
    {
        _logger.LogInformation("User {user} requesting capture list", userId);
        
        var uploads = await _db.GetUploads(userId);
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

        return ApiResult<CaptureListResponse>.Success(new CaptureListResponse { Captures = elements });
    }
}