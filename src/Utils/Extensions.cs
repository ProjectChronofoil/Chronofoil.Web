using System.Security.Claims;
using Chronofoil.Common;
using Microsoft.AspNetCore.Mvc;

namespace Chronofoil.Web.Utils;

public static class Extensions
{
    public static Guid GetCfUserId(this ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) throw new UnauthorizedAccessException("Failed to get user claim from valid token.");
        return Guid.Parse(userId);
    }
    
    public static ActionResult ToActionResult(this ApiResult result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result);
        }
        
        var statusCode = result.StatusCode.ToHttpStatusCode();
        return new ObjectResult(result) { StatusCode = statusCode };
    }
    
    public static ActionResult<ApiResult<T>> ToActionResult<T>(this ApiResult<T> result) where T : class
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result);
        }
        
        var statusCode = result.StatusCode.ToHttpStatusCode();
        return new ObjectResult(result) { StatusCode = statusCode };
    }

}