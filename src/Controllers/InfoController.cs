using Chronofoil.Common.Auth;
using Chronofoil.Common.Info;
using Chronofoil.Web.Services.Auth;
using Chronofoil.Web.Services.Info;
using Microsoft.AspNetCore.Mvc;

namespace Chronofoil.Web.Controllers;

[ApiController]
[Route("api")]
public class InfoController : Controller
{
    private readonly ILogger _log;
    private readonly InfoService _infoService;
    
    public InfoController(ILogger<AuthController> log, InfoService infoService)
    {
        _log = log;
        _infoService = infoService;
    }

    [HttpGet("info/tos")]
    public async Task<ActionResult<TosResponse>> GetTos()
    {
        _log.LogInformation("TOS");
        
        try
        {
            return _infoService.GetCurrentTos();
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to get TOS.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
    
    [HttpGet("info/faq")]
    public async Task<ActionResult<FaqResponse>> GetFaq()
    {
        _log.LogInformation("FAQ");
        
        try
        {
            return _infoService.GetCurrentFaq();
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to get FAQ.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
    
}