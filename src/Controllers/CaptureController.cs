using Chronofoil.Common.Capture;
using Chronofoil.Web.Services.Capture;
using Chronofoil.Web.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Chronofoil.Web.Controllers;

[ApiController]
[Route("api/capture")]
public class CaptureController : Controller
{
    private readonly ILogger<CaptureController> _log;
    private readonly CaptureService _captureService;

    public CaptureController(ILogger<CaptureController> log, CaptureService captureService)
    {
        _log = log;
        _captureService = captureService;
    }
    
    // [Authorize]
    // [HttpPost("ticket")]
    // public async Task<IActionResult> RequestUploadTicket([FromBody] UploadTicketRequest request)
    // {
    //     try
    //     {
    //         var userId = User.GetCfUserId();
    //         if (userId == null) return Forbid();
    //         var result = await _uploadService.GetUploadTicket(userId.Value, request);
    //         return result.Match<IActionResult>(
    //             ticket => Ok(new { Ticket = ticket }),
    //             statusCode => statusCode
    //         );
    //     }
    //     catch (Exception ex) 
    //     {
    //         _log.LogError(ex, "Failed to register.");
    //         return StatusCode(StatusCodes.Status500InternalServerError);
    //     }
    // }

    [Authorize]
    [HttpPost("upload")]
    [RequestSizeLimit(209_715_200)]
    public async Task<ActionResult<CaptureUploadResponse>> UploadFile(List<IFormFile?>? files)
    {
        try
        {
            if (files == null || files.Any(file => file == null || file.Length == 0)) return BadRequest("Empty files are not allowed.");

            var meta = files.FirstOrDefault(file => file!.FileName == "meta.json");
            var capture = files.FirstOrDefault(file => file!.FileName.EndsWith(".ccfcap"));

            if (meta == null || capture == null) return BadRequest("Files must conform to Chronofoil API spec.");

            string metaStr;
            await using (var stream = meta.OpenReadStream())
            using (var reader = new StreamReader(stream)) 
                metaStr = await reader.ReadToEndAsync();
            var request = JsonSerializer.Deserialize<CaptureUploadRequest>(metaStr);
            if (request == null) return BadRequest("Failed to understand metadata.");

            var userId = User.GetCfUserId();
            var result = await _captureService.Upload(userId, request, capture);
            
            return result.Match<ActionResult<CaptureUploadResponse>>(
                response => Accepted(response),
                statusCode => statusCode
            );
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to process upload.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
    
    [Authorize]
    [HttpPost("delete")]
    public async Task<IActionResult> DeleteCapture(CaptureDeletionRequest request)
    {
        try
        {
            var userId = User.GetCfUserId();
            var result = await _captureService.Delete(userId, request.CaptureId);
            return result;
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to process deletion request.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
    
    [Authorize]
    [HttpGet("list")]
    public async Task<ActionResult<CaptureListResponse>> GetCaptureList()
    {
        try
        {
            var userId = User.GetCfUserId();
            var result = await _captureService.GetCaptures(userId);
            return Ok(result);
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to process capture list request.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}