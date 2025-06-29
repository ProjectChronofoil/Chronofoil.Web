using Chronofoil.Common;
using Chronofoil.Common.Auth;
using Chronofoil.Web.Services.Auth;
using Chronofoil.Web.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronofoil.Web.Controllers;

[ApiController]
[Route(Routes.AuthV1)]
public class AuthController : Controller
{
    private readonly ILogger _log;
    private readonly IAuthService _authService;
    
    public AuthController(ILogger<AuthController> log, IAuthService authService)
    {
        _log = log;
        _authService = authService;
    }

    [HttpPost("register/{provider}")]
    public async Task<ActionResult<ApiResult<AccessTokenResponse>>> Register(string provider, AuthRequest request)
    {
        _log.LogInformation("Register");
        
        try
        {
            var result = await _authService.Register(provider, request.AuthorizationCode);
            return result.ToActionResult();
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to register.");
            return ApiResult<AccessTokenResponse>.Failure().ToActionResult();
        }
    }
    
    [HttpPost("login/{provider}")]
    public async Task<ActionResult<ApiResult<AccessTokenResponse>>> Login(string provider, AuthRequest request)
    {
        _log.LogInformation("Login");
        
        try
        {
            var result = await _authService.Login(provider, request.AuthorizationCode);
            return result.ToActionResult();
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to login.");
            return ApiResult<AccessTokenResponse>.Failure().ToActionResult();
        }
    }
    
    [HttpPost("token/refresh")]
    public async Task<ActionResult<ApiResult<AccessTokenResponse>>> RefreshToken(RefreshRequest request) 
    {
        _log.LogInformation("RefreshToken");
        
        try
        {
            var result = await _authService.RefreshToken(request.RefreshToken);
            return result.ToActionResult();
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to generate refresh token.");
            return ApiResult<AccessTokenResponse>.Failure().ToActionResult();
        }
    }
    
    [Authorize]
    [HttpPost("tos/accept")]
    public async Task<ActionResult<ApiResult>> AcceptTos([FromQuery] int tosVersion) 
    {
        _log.LogInformation("AcceptTos");
        
        try
        {
            var userId = User.GetCfUserId();
            var result = await _authService.AcceptTosVersion(userId, tosVersion);
            return result.ToActionResult();
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to write accept TOS.");
            return ApiResult.Failure().ToActionResult();
        }
    }
}