using Chronofoil.Common;
using Chronofoil.Common.Auth;
using Chronofoil.Web.Services.Auth.External;

namespace Chronofoil.Web.Services.Auth;

public interface IAuthService
{
    Task<(AccessTokenResponse? response, IExternalAuthService.UserInfo? userInfo)> AuthUser(string provider, string authCode);

    Task<(AccessTokenResponse? response, IExternalAuthService.UserInfo? userInfo)> RefreshUser(string provider, string refreshCode);

    string GenerateRefreshToken();

    string GenerateJwtToken(Guid cfUserId, string userName, string provider, TimeSpan tokenDuration);

    Task<ApiResult<AccessTokenResponse>> Register(string provider, string authCode);
   
    Task<ApiResult<AccessTokenResponse>> Login(string provider, string authCode);
    
    Task<ApiResult<AccessTokenResponse>> RefreshToken(string refreshToken);
    
    Task<ApiResult> AcceptTosVersion(Guid userId, int version);
}