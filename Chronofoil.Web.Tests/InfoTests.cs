using Chronofoil.Common;
using Chronofoil.Common.Info;
using Shouldly;
using Xunit;

namespace Chronofoil.Web.Tests;

[Trait("Category", "UnitTest")]
public class InfoTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    [Fact]
    public async Task Info_Tos()
    {
        var expected = new TosResponse { Version = 1, Text = "Test Terms of Service" };
        fixture.InfoServiceMock
            .Setup(s => s.GetCurrentTos())
            .Returns(ApiResult<TosResponse>.Success(expected));

        var result = await fixture.ApiClient.GetTos();

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data.Version.ShouldBe(expected.Version);
        result.Data.Text.ShouldBe(expected.Text);
    }

    [Fact]
    public async Task Info_Faq()
    {
        var expected = new FaqResponse { Entries = [new FaqEntry { Question = "Q", Answer = "A" }] };
        fixture.InfoServiceMock
            .Setup(s => s.GetCurrentFaq())
            .Returns(ApiResult<FaqResponse>.Success(expected));

        var result = await fixture.ApiClient.GetFaq();

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data.Entries.ShouldBeEquivalentTo(expected.Entries);
    }
}