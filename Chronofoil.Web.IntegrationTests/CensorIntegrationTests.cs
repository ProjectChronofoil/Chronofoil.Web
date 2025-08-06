using System.ComponentModel;
using Chronofoil.Common;
using Chronofoil.Common.Auth;
using Chronofoil.Common.Censor;
using Shouldly;
using Xunit;

namespace Chronofoil.Web.IntegrationTests;

[Trait("Category", "IntegrationTest")]
public class CensorIntegrationTests : IClassFixture<ApiIntegrationTestFixture>
{
    private readonly ApiIntegrationTestFixture _fixture;
    private const string GameVersion = "2025.05.17.0000.0000";

    public CensorIntegrationTests(ApiIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _ = _fixture.Respawner.ResetAsync();
    }

    private async Task<string> GetAuthToken()
    {
        var request = new AuthRequest { AuthorizationCode = "good_auth_code" };
        var result = await _fixture.ApiClient.Register("testProvider", request);
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data.ShouldNotBeNull();
        return result.Data.AccessToken;
    }

    [Fact]
    public async Task Test_FoundOpcodes_Failure_UnknownGameVersion()
    {
        var token = await GetAuthToken();
        var request = new FoundOpcodesRequest
        {
            GameVersion = "invalid-version",
            Opcodes = new Dictionary<string, int> { { "ZoneLetterListDown", 1 } }
        };

        var result = await _fixture.ApiClient.FoundOpcodes(token, request);

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.OpcodeUnknownGameVersion);
    }

    [Fact]
    public async Task Test_FoundOpcodes_Failure_InvalidOpcodeKey()
    {
        var token = await GetAuthToken();
        var request = new FoundOpcodesRequest
        {
            GameVersion = GameVersion,
            Opcodes = new Dictionary<string, int> { { "BadOpcode", 123 } }
        };

        var result = await _fixture.ApiClient.FoundOpcodes(token, request);

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.OpcodeInvalidKey);
    }
    
    [Fact]
    public async Task Test_FoundOpcodes_Failure_OpcodeMismatch()
    {
        var token = await GetAuthToken();
        var request1 = new FoundOpcodesRequest
        {
            GameVersion = GameVersion,
            Opcodes = new Dictionary<string, int> { { "ZoneLetterListDown", 100 } }
        };

        var result1 = await _fixture.ApiClient.FoundOpcodes(token, request1);
        result1.StatusCode.ShouldBe(ApiStatusCode.Success);
        
        var request2 = new FoundOpcodesRequest
        {
            GameVersion = GameVersion,
            Opcodes = new Dictionary<string, int> { { "ZoneLetterListDown", 200 } }
        };
        
        var result2 = await _fixture.ApiClient.FoundOpcodes(token, request2);

        result2.ShouldNotBeNull();
        result2.StatusCode.ShouldBe(ApiStatusCode.OpcodeMismatchWithKnown);
    }

    [Fact]
    public async Task Test_FoundOpcodes_Success()
    {
        var token = await GetAuthToken();
        var request = new FoundOpcodesRequest
        {
            GameVersion = GameVersion,
            Opcodes = new Dictionary<string, int>
            {
                { "ZoneLetterListDown", 123 }
            }
        };

        var result = await _fixture.ApiClient.FoundOpcodes(token, request);

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
    }
    
    [Fact]
    public async Task Test_GetOpcodes_Success_NoOpcodes()
    {
        var token = await GetAuthToken();
        var result = await _fixture.ApiClient.GetOpcodes(token, GameVersion);
        
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data.ShouldNotBeNull();
        result.Data.GameVersion.ShouldBe(GameVersion);
        result.Data.Opcodes.ShouldBeEmpty();
    }
    
    [Fact]
    public async Task Test_GetOpcodes_Success_WithOpcodes()
    {
        var token = await GetAuthToken();
        var opcodes = new Dictionary<string, int>
        {
            { "ZoneLetterListDown", 123 },
            { "ZoneLetterUp", 456 }
        };
        var request = new FoundOpcodesRequest
        {
            GameVersion = GameVersion,
            Opcodes = opcodes
        };

        var foundResult = await _fixture.ApiClient.FoundOpcodes(token, request);
        foundResult.StatusCode.ShouldBe(ApiStatusCode.Success);
        
        var getResult = await _fixture.ApiClient.GetOpcodes(token, GameVersion);
        
        getResult.ShouldNotBeNull();
        getResult.StatusCode.ShouldBe(ApiStatusCode.Success);
        getResult.Data.ShouldNotBeNull();
        getResult.Data.GameVersion.ShouldBe(GameVersion);
        getResult.Data.Opcodes.ShouldBe(opcodes);
    }
}