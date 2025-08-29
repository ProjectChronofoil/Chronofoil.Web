using Chronofoil.Common;
using Chronofoil.Common.Auth;
using Moq;
using Shouldly;
using Xunit;

namespace Chronofoil.Web.Tests;

[Trait("Category", "UnitTest")]
public class AuthTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    [Fact]
    public async Task Auth_Register_Provider()
    {
        var request = new AuthRequest { AuthorizationCode = "test_code" };
        var expected = new AccessTokenResponse
            { AccessToken = "access_token", RefreshToken = "refresh_token", ExpiresIn = 3600 };
        fixture.AuthServiceMock
            .Setup(s => s.Register("testAuthProvider", request.AuthorizationCode))
            .ReturnsAsync(ApiResult<AccessTokenResponse>.Success(expected));

        var result = await fixture.ApiClient.Register("testAuthProvider", request);

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data.AccessToken.ShouldBe(expected.AccessToken);
        result.Data.RefreshToken.ShouldBe(expected.RefreshToken);
        result.Data.ExpiresIn.ShouldBe(expected.ExpiresIn);
    }

    [Fact]
    public async Task Auth_Login_Provider()
    {
        var request = new AuthRequest { AuthorizationCode = "test_code" };
        var expected = new AccessTokenResponse
            { AccessToken = "access_token", RefreshToken = "refresh_token", ExpiresIn = 3600 };
        fixture.AuthServiceMock
            .Setup(s => s.Login("testAuthProvider", request.AuthorizationCode))
            .ReturnsAsync(ApiResult<AccessTokenResponse>.Success(expected));

        var result = await fixture.ApiClient.Login("testAuthProvider", request);

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data.AccessToken.ShouldBe(expected.AccessToken);
        result.Data.RefreshToken.ShouldBe(expected.RefreshToken);
        result.Data.ExpiresIn.ShouldBe(expected.ExpiresIn);
    }

    [Fact]
    public async Task Auth_Token_Refresh()
    {
        var request = new RefreshRequest { RefreshToken = "test_refresh_token" };
        var expected = new AccessTokenResponse
            { AccessToken = "new_access_token", RefreshToken = "new_refresh_token", ExpiresIn = 3600 };
        fixture.AuthServiceMock
            .Setup(s => s.RefreshToken(request.RefreshToken))
            .ReturnsAsync(ApiResult<AccessTokenResponse>.Success(expected));

        var result = await fixture.ApiClient.RefreshToken(request);

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data.AccessToken.ShouldBe(expected.AccessToken);
        result.Data.RefreshToken.ShouldBe(expected.RefreshToken);
        result.Data.ExpiresIn.ShouldBe(expected.ExpiresIn);
    }

    [Fact]
    public async Task Auth_Tos_Accept()
    {
        fixture.AuthServiceMock
            .Setup(s => s.AcceptTosVersion(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(ApiResult.Success());

        var result = await fixture.ApiClient.AcceptTos("unused", 1);

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
    }
}