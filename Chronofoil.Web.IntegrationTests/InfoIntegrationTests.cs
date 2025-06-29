using Chronofoil.Common;
using Shouldly;
using Xunit;

namespace Chronofoil.Web.IntegrationTests;

[Trait("Category", "IntegrationTest")]
public class InfoIntegrationTests : IClassFixture<ApiIntegrationTestFixture>
{
    private readonly ApiIntegrationTestFixture _fixture;

    public InfoIntegrationTests(ApiIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Test_GetTos_Success()
    {
        var result = await _fixture.ApiClient.GetTos();

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data.ShouldNotBeNull();
        result.Data.Version.ShouldBe(1);
        result.Data.Text.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Test_GetFaq_Success()
    {
        var result = await _fixture.ApiClient.GetFaq();

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data.ShouldNotBeNull();
        result.Data!.Entries.ShouldNotBeNull();
        result.Data!.Entries.ShouldNotBeEmpty();
    }
}