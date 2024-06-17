using Chronofoil.CaptureFile.Binary;
using Chronofoil.Common.Auth;
using Chronofoil.Web.Services.Auth;
using Chronofoil.Web.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronofoil.Web.Controllers;

[ApiController]
[Route("api")]
public class AuthController : Controller
{
    private readonly ILogger _log;
    private readonly AuthService _authService;
    
    public AuthController(ILogger<AuthController> log, AuthService authService)
    {
        _log = log;
        _authService = authService;
    }

    [HttpPost("register/{provider}")]
    public async Task<ActionResult<AccessTokenResponse>> Register(string provider, AuthRequest request)
    {
        _log.LogDebug("Register");
        
        try
        {
            var result = await _authService.Register(provider, request.AuthorizationCode);

            return result.Match<ActionResult<AccessTokenResponse>>(
                response => Ok(response),
                statusCode => statusCode);
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to register.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
    
    [HttpPost("login/{provider}")]
    public async Task<ActionResult<AccessTokenResponse>> Login(string provider, AuthRequest request)
    {
        _log.LogDebug("Login");
        
        try
        {
            var result = await _authService.Login(provider, request.AuthorizationCode);

            return result.Match<ActionResult<AccessTokenResponse>>(
                response => Ok(response),
                statusCode => statusCode);
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to login.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
    
    [HttpPost("token/refresh")]
    public async Task<ActionResult<AccessTokenResponse>> RefreshToken(RefreshRequest request) 
    {
        _log.LogDebug("RefreshToken");
        
        try
        {
            var result = await _authService.RefreshToken(request.RefreshToken);

            return result.Match<ActionResult<AccessTokenResponse>>(
                response => Ok(response),
                statusCode => statusCode);
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to generate refresh token.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
    
    [Authorize]
    [HttpPost("tos/accept")]
    public async Task<ActionResult> AcceptTos(AcceptTosRequest request) 
    {
        _log.LogDebug("AcceptTos");
        
        try
        {
            var userId = User.GetCfUserId();
            // var userId = request.UserId;
            var result = await _authService.AcceptTosVersion(userId, request.TosVersion);
            return result;
        }
        catch (Exception ex) 
        {
            _log.LogError(ex, "Failed to write accept TOS.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}