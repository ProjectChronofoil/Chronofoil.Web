using Chronofoil.Common.Auth;
using Chronofoil.Web.Services.Auth.External;

namespace Chronofoil.Web.IntegrationTests;

public class MockExternalAuthService : IExternalAuthService
{
    public static readonly Guid TestUserId = Guid.NewGuid();
    public static readonly Guid TestUserId2 = Guid.NewGuid();
    
    private readonly AccessTokenResponse _goodResponse;
    private readonly AccessTokenResponse _goodResponse2;
    private readonly AccessTokenResponse _badResponse;
    private readonly AccessTokenResponse _badResponseRefreshTokenOnly;

    public MockExternalAuthService()
    {
        _goodResponse = new AccessTokenResponse
        {
            AccessToken = "good_access_token",
            RefreshToken = "good_refresh_token",
            ExpiresIn = 3600
        };
        
        _goodResponse2 = new AccessTokenResponse
        {
            AccessToken = "good_access_token2",
            RefreshToken = "good_refresh_token2",
            ExpiresIn = 3600
        };

        _badResponse = new AccessTokenResponse
        {
            AccessToken = "bad_access_token",
            RefreshToken = "bad_refresh_token",
            ExpiresIn = 3600
        };

        _badResponseRefreshTokenOnly = new AccessTokenResponse
        {
            AccessToken = "good_access_token",
            RefreshToken = "bad_refresh_token",
            ExpiresIn = 3600
        };
    }
    
    public Task<AccessTokenResponse?> ExchangeCodeForTokenAsync(string code)
    {
        return (code switch
        {
            "good_auth_code" => Task.FromResult(_goodResponse),
            "good_auth_code2" => Task.FromResult(_goodResponse2),
            "bad_refresh_auth_code" => Task.FromResult(_badResponseRefreshTokenOnly),
            "bad_auth_code" => Task.FromResult(_badResponse),
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        })!;
    }

    public Task<AccessTokenResponse?> ExchangeRefreshCodeForTokenAsync(string code)
    {
        return (code switch
        {
            "good_refresh_token" => Task.FromResult(_goodResponse),
            "bad_refresh_token" => Task.FromResult(_badResponse),
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        })!;
    }

    public Task<IExternalAuthService.UserInfo?> GetUserInfoAsync(string accessToken)
    {
        var userInfo = new IExternalAuthService.UserInfo("testProvider", "testUser", TestUserId.ToString());
        var userInfo2 = new IExternalAuthService.UserInfo("testProvider", "testUser2", TestUserId2.ToString());
        
        return (accessToken switch
        {
            "good_access_token" => Task.FromResult(userInfo),
            "good_access_token2" => Task.FromResult(userInfo2),
            "bad_access_token" => Task.FromResult<IExternalAuthService.UserInfo?>(null)!,
            _ => throw new ArgumentOutOfRangeException(nameof(accessToken), accessToken, null)
        })!;
    }
}