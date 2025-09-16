using Chronofoil.Common;
using Chronofoil.Common.Censor;
using Chronofoil.Web.Services.Censor;
using Chronofoil.Web.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronofoil.Web.Controllers;

[ApiController]
[Route(Routes.CensorV1)]
public class CensorController : Controller
{
    private readonly ILogger<CensorController> _log;
    private readonly ICensorService _censorService;

    public CensorController(ILogger<CensorController> log, ICensorService censorService)
    {
        _log = log;
        _censorService = censorService;
    }
    
    [Authorize]
    [HttpPost("found")]
    public async Task<ActionResult<ApiResult>> FoundOpcodes(FoundOpcodesRequest request)
    {
        _log.LogInformation("[FoundOpcodes] request: {request}", request);
        try
        {
            var userId = User.GetCfUserId();
            var result = await _censorService.ProcessFoundOpcodes(userId, request);
            return result.ToActionResult();
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to process opcode request.");
            return ApiResult.Failure().ToActionResult();
        }
    }
    
    [HttpGet("opcodes")]
    public async Task<ActionResult<ApiResult<CensoredOpcodesResponse>>> GetOpcodes([FromQuery] string gameVersion)
    {
        _log.LogInformation("[GetOpcodes] gameVersion: {version}", gameVersion);
        try
        {
            var result = await _censorService.GetCurrentOpcodes(gameVersion);
            return result.ToActionResult();
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to process opcode request.");
            return ApiResult<CensoredOpcodesResponse>.Failure().ToActionResult();
        }
    }
}