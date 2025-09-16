using Chronofoil.Common;
using Chronofoil.Common.Censor;
using Moq;
using Shouldly;
using Xunit;

namespace Chronofoil.Web.Tests;

[Trait("Category", "UnitTest")]
public class CensorTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    [Fact]
    public async Task Censor_Found()
    {
        var request = new FoundOpcodesRequest
        {
            GameVersion = "2012.01.01.0000.0000",
            Opcodes = new Dictionary<string, int> { { "Opcode", 0xABC } }
        };
        fixture.CensorServiceMock
            .Setup(s => s.ProcessFoundOpcodes(It.IsAny<Guid>(), It.IsAny<FoundOpcodesRequest>()))
            .ReturnsAsync(ApiResult.Success());

        var result = await fixture.ApiClient.FoundOpcodes("unused", request);

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
    }

    [Fact]
    public async Task Censor_Opcodes()
    {
        var gameVersion = "2012.01.01.0000.0000";
        var expected = new CensoredOpcodesResponse
        {
            GameVersion = gameVersion,
            Opcodes = new Dictionary<string, int> { { "Opcode", 0xABC } }
        };
        fixture.CensorServiceMock
            .Setup(s => s.GetCurrentOpcodes(gameVersion))
            .ReturnsAsync(ApiResult<CensoredOpcodesResponse>.Success(expected));

        var result = await fixture.ApiClient.GetOpcodes(gameVersion);

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data.GameVersion.ShouldBe(expected.GameVersion);
        result.Data.Opcodes.ShouldBeEquivalentTo(expected.Opcodes);
    }
}