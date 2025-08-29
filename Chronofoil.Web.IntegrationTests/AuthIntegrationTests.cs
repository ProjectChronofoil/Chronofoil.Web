using Chronofoil.Common;
using Chronofoil.Common.Auth;
using Shouldly;
using Xunit;

namespace Chronofoil.Web.IntegrationTests;

[Trait("Category", "IntegrationTest")]
public class AuthIntegrationTests : IClassFixture<ApiIntegrationTestFixture>
{
    private readonly ApiIntegrationTestFixture _fixture;

    public AuthIntegrationTests(ApiIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _ = _fixture.Respawner.ResetAsync();
    }
    
    [Fact]
    public async Task Test_Register_Failure_AuthProviderFailure()
    {
        var request = new AuthRequest { AuthorizationCode = "bad_auth_code" };
        
        var result = await _fixture.ApiClient.Register("testProvider", request);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.AuthProviderAuthFailure);
    }

    [Fact]
    public async Task Test_Register_Failure_UserExists()
    {
        var request = new AuthRequest { AuthorizationCode = "good_auth_code" };
        
        var result = await _fixture.ApiClient.Register("testProvider", request);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data!.AccessToken.ShouldNotBeNull();
        result.Data!.RefreshToken.ShouldNotBeNull();
        result.Data!.ExpiresIn.ShouldBeGreaterThan(0);
        
        result = await _fixture.ApiClient.Register("testProvider", request);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.UserExists);
    }
    
    [Fact]
    public async Task Test_Register_Failure_UnknownError()
    {
        var request = new AuthRequest { AuthorizationCode = "unknown_auth_code" };
        
        var result = await _fixture.ApiClient.Register("testProvider", request);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.UnknownError);
    }
    
    [Fact]
    public async Task Test_Register_Success()
    {
        var request = new AuthRequest { AuthorizationCode = "good_auth_code" };
        
        var result = await _fixture.ApiClient.Register("testProvider", request);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data!.AccessToken.ShouldNotBeNull();
        result.Data!.RefreshToken.ShouldNotBeNull();
        result.Data!.ExpiresIn.ShouldBeGreaterThan(0);
    }
    
    [Fact]
    public async Task Test_Login_Failure_AuthProviderFailure()
    {
        var request = new AuthRequest { AuthorizationCode = "good_auth_code" };
        
        var result = await _fixture.ApiClient.Register("testProvider", request);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data!.AccessToken.ShouldNotBeNull();
        result.Data!.RefreshToken.ShouldNotBeNull();
        result.Data!.ExpiresIn.ShouldBeGreaterThan(0);

        var request2 = new AuthRequest { AuthorizationCode = "bad_auth_code" };
        
        result = await _fixture.ApiClient.Login("testProvider", request2);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.AuthProviderAuthFailure);
    }
    
    [Fact]
    public async Task Test_Login_Failure_UnknownError()
    {
        var request = new AuthRequest { AuthorizationCode = "good_auth_code" };
        
        var result = await _fixture.ApiClient.Register("testProvider", request);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data!.AccessToken.ShouldNotBeNull();
        result.Data!.RefreshToken.ShouldNotBeNull();
        result.Data!.ExpiresIn.ShouldBeGreaterThan(0);

        var request2 = new AuthRequest { AuthorizationCode = "unknown_auth_code" };
        
        result = await _fixture.ApiClient.Login("testProvider", request2);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.UnknownError);
    }

    [Fact]
    public async Task Test_Login_Success()
    {
        var request = new AuthRequest { AuthorizationCode = "good_auth_code" };
        
        var result = await _fixture.ApiClient.Register("testProvider", request);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data!.AccessToken.ShouldNotBeNull();
        result.Data!.RefreshToken.ShouldNotBeNull();
        result.Data!.ExpiresIn.ShouldBeGreaterThan(0);
        
        result = await _fixture.ApiClient.Login("testProvider", request);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
    }
    
    [Fact]
    public async Task Test_Refresh_Failure_UserTokenNotFound()
    {
        var request = new AuthRequest { AuthorizationCode = "good_auth_code" };
        
        var result = await _fixture.ApiClient.Register("testProvider", request);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data!.AccessToken.ShouldNotBeNull();
        result.Data!.RefreshToken.ShouldNotBeNull();
        result.Data!.ExpiresIn.ShouldBeGreaterThan(0);

        var refreshRequest = new RefreshRequest { RefreshToken = "unknown_refresh_token" };

        result = await _fixture.ApiClient.RefreshToken(refreshRequest);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.UserTokenNotFound);
        result.Data.ShouldBeNull();
    }

    [Fact]
    public async Task Test_Refresh_Failure_AuthProviderFailure()
    {
        var request = new AuthRequest { AuthorizationCode = "bad_refresh_auth_code" };
        
        var result = await _fixture.ApiClient.Register("testProvider", request);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data!.AccessToken.ShouldNotBeNull();
        result.Data!.RefreshToken.ShouldNotBeNull();
        result.Data!.ExpiresIn.ShouldBeGreaterThan(0);

        var refreshRequest = new RefreshRequest { RefreshToken = result.Data!.RefreshToken };

        result = await _fixture.ApiClient.RefreshToken(refreshRequest);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.AuthProviderRefreshFailure);
        result.Data.ShouldBeNull();
    }
    
    [Fact]
    public async Task Test_Refresh_Success()
    {
        var request = new AuthRequest { AuthorizationCode = "good_auth_code" };
        
        var result = await _fixture.ApiClient.Register("testProvider", request);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data!.AccessToken.ShouldNotBeNull();
        result.Data!.RefreshToken.ShouldNotBeNull();
        result.Data!.ExpiresIn.ShouldBeGreaterThan(0);

        var refreshRequest = new RefreshRequest { RefreshToken = result.Data!.RefreshToken };

        result = await _fixture.ApiClient.RefreshToken(refreshRequest);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data!.AccessToken.ShouldNotBeNull();
        result.Data!.RefreshToken.ShouldNotBeNull();
        result.Data!.ExpiresIn.ShouldBeGreaterThan(0);
    }
    
    [Fact]
    public async Task Test_AcceptTos_Failure_UserNotFound()
    {
        var request = new AuthRequest { AuthorizationCode = "good_auth_code" };
        
        var result = await _fixture.ApiClient.Register("testProvider", request);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data!.AccessToken.ShouldNotBeNull();
        result.Data!.RefreshToken.ShouldNotBeNull();
        result.Data!.ExpiresIn.ShouldBeGreaterThan(0);

        // Stupid scenario but it's easy to test
        await _fixture.Respawner.ResetAsync();

        var result2 = await _fixture.ApiClient.AcceptTos(result.Data!.AccessToken, 1);
        
        result2.ShouldNotBeNull();
        result2.StatusCode.ShouldBe(ApiStatusCode.UserNotFound);
    }
    
    [Fact]
    public async Task Test_AcceptTos_Failure_InvalidTosVersion()
    {
        var request = new AuthRequest { AuthorizationCode = "good_auth_code" };
        
        var result = await _fixture.ApiClient.Register("testProvider", request);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data!.AccessToken.ShouldNotBeNull();
        result.Data!.RefreshToken.ShouldNotBeNull();
        result.Data!.ExpiresIn.ShouldBeGreaterThan(0);

        var tosResult = await _fixture.ApiClient.GetTos();
        
        tosResult.ShouldNotBeNull();
        tosResult.StatusCode.ShouldBe(ApiStatusCode.Success);
        tosResult.Data!.ShouldNotBeNull();
        
        var currentVersion = tosResult.Data!.Version; 

        var result2 = await _fixture.ApiClient.AcceptTos(result.Data!.AccessToken, currentVersion - 1);
        
        result2.ShouldNotBeNull();
        result2.StatusCode.ShouldBe(ApiStatusCode.AuthInvalidTosVersion);
        
        var result3 = await _fixture.ApiClient.AcceptTos(result.Data!.AccessToken, currentVersion + 1);
        
        result3.ShouldNotBeNull();
        result3.StatusCode.ShouldBe(ApiStatusCode.AuthInvalidTosVersion);
    }

    [Fact]
    public async Task Test_AcceptTos_Success()
    {
        var request = new AuthRequest { AuthorizationCode = "good_auth_code" };
        
        var result = await _fixture.ApiClient.Register("testProvider", request);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data!.AccessToken.ShouldNotBeNull();
        result.Data!.RefreshToken.ShouldNotBeNull();
        result.Data!.ExpiresIn.ShouldBeGreaterThan(0);

        var result2 = await _fixture.ApiClient.AcceptTos(result.Data!.AccessToken, 1);
        
        result2.ShouldNotBeNull();
        result2.StatusCode.ShouldBe(ApiStatusCode.Success);
    }


}