using Chronofoil.Common;
using Chronofoil.Common.Info;
using Chronofoil.Web.Services.Info;
using Chronofoil.Web.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Chronofoil.Web.Controllers;

[ApiController]
[Route(Routes.InfoV1)]
public class InfoController : Controller
{
    private readonly ILogger _log;
    private readonly IInfoService _infoService;
    
    public InfoController(ILogger<AuthController> log, IInfoService infoService)
    {
        _log = log;
        _infoService = infoService;
    }

    [HttpGet("tos")]
    public async Task<ActionResult<ApiResult<TosResponse>>> GetTos()
    {
        _log.LogInformation("TOS");
        
        try
        {
            return _infoService.GetCurrentTos().ToActionResult();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to get TOS.");
            return ApiResult<TosResponse>.Failure().ToActionResult();
        }
    }
    
    [HttpGet("faq")]
    public async Task<ActionResult<ApiResult<FaqResponse>>> GetFaq()
    {
        _log.LogInformation("FAQ");
        
        try
        {
            return _infoService.GetCurrentFaq().ToActionResult();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to get FAQ.");
            return ApiResult<FaqResponse>.Failure().ToActionResult();
        }
    }
    
}