using Chronofoil.Common.Auth;

namespace Chronofoil.Web.Services.Auth.External;

public interface IExternalAuthService
{
    public record UserInfo(
        string Provider,
        string Username,
        string UserId);
    
    public Task<AccessTokenResponse> ExchangeCodeForTokenAsync(string code);

    public Task<AccessTokenResponse> ExchangeRefreshCodeForTokenAsync(string code);

    public Task<UserInfo> GetUserInfoAsync(string accessToken);
}