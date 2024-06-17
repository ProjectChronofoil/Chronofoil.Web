using Chronofoil.Common.Censor;
using Chronofoil.Web.Services.Censor;
using Chronofoil.Web.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronofoil.Web.Controllers;

[ApiController]
[Route("api/censor")]
public class CensorController : Controller
{
    private readonly ILogger<CensorController> _log;
    private readonly CensorService _censorService;

    public CensorController(ILogger<CensorController> log, CensorService censorService)
    {
        _log = log;
        _censorService = censorService;
    }
    
    [Authorize]
    [HttpPost("found")]
    public async Task<IActionResult> FoundOpcodes([FromBody] FoundOpcodesRequest request)
    {
        _log.LogInformation("[FoundOpcodes] request: {request}", request);
        try
        {
            // var userId = User.GetCfUserId();
            var userId = Guid.NewGuid();
            var result = await _censorService.ProcessFoundOpcodes(userId, request);
            return result ? Ok() : BadRequest();
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to process opcode request.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
    
    [Authorize]
    [HttpGet("opcodes")]
    public async Task<ActionResult<CensoredOpcodesResponse>> GetOpcodes(CensoredOpcodesRequest request)
    {
        _log.LogInformation("[GetOpcodes] request: {request}", request);
        try
        {
            var result = await _censorService.GetCurrentOpcodes(request.GameVersion);
            return Ok(result);
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to process opcode request.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}