using Chronofoil.Common;
using Chronofoil.Common.Capture;
using Chronofoil.Web.Services.Capture;
using Chronofoil.Web.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Chronofoil.Web.Controllers;

[ApiController]
[Route(Routes.CaptureV1)]
public class CaptureController : Controller
{
    private readonly ILogger<CaptureController> _log;
    private readonly ICaptureService _captureService;

    public CaptureController(ILogger<CaptureController> log, ICaptureService captureService)
    {
        _log = log;
        _captureService = captureService;
    }
    
    [Authorize]
    [HttpPost("upload")]
    [RequestSizeLimit(209_715_200)]
    public async Task<ActionResult<ApiResult<CaptureUploadResponse>>> UploadFile(List<IFormFile?>? files)
    {
        try
        {
            if (files == null || files.Any(file => file == null || file.Length == 0))
                return ApiResult<CaptureUploadResponse>.Failure(ApiStatusCode.FormFileIsEmptyFile).ToActionResult();

            var meta = files.FirstOrDefault(file => file!.FileName == "meta.json");
            var capture = files.FirstOrDefault(file => file!.FileName.EndsWith(".ccfcap"));

            if (meta == null || capture == null)
                return ApiResult<CaptureUploadResponse>.Failure(ApiStatusCode.FormFileNotValid).ToActionResult();

            string metaStr;
            await using (var stream = meta.OpenReadStream())
            using (var reader = new StreamReader(stream)) 
                metaStr = await reader.ReadToEndAsync();
            var request = JsonSerializer.Deserialize<CaptureUploadRequest>(metaStr);
            if (request == null)
                return ApiResult<CaptureUploadResponse>.Failure(ApiStatusCode.FormFileMetadataNotValid).ToActionResult();

            var userId = User.GetCfUserId();
            var result = await _captureService.Upload(userId, request, capture);
            return result.ToActionResult();
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to process upload.");
            return ApiResult<CaptureUploadResponse>.Failure().ToActionResult();
        }
    }
    
    [Authorize]
    [HttpPost("delete")]
    public async Task<ActionResult<ApiResult>> DeleteCapture([FromQuery] Guid captureId)
    {
        try
        {
            var userId = User.GetCfUserId();
            var result = await _captureService.Delete(userId, captureId);
            return result.ToActionResult();
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to process deletion request.");
            return ApiResult.Failure().ToActionResult();
        }
    }
    
    [Authorize]
    [HttpGet("list")]
    public async Task<ActionResult<ApiResult<CaptureListResponse>>> GetCaptureList()
    {
        try
        {
            var userId = User.GetCfUserId();
            var result = await _captureService.GetCaptures(userId);
            return result.ToActionResult();
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to process capture list request.");
            return ApiResult<CaptureListResponse>.Failure().ToActionResult();
        }
    }
}