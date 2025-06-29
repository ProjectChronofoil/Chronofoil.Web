using System.Text;
using System.Text.Json;
using Chronofoil.Common;
using Chronofoil.Common.Capture;
using Microsoft.AspNetCore.Http;
using Moq;
using Refit;
using Shouldly;
using Xunit;

namespace Chronofoil.Web.Tests;

[Trait("Category", "UnitTest")]
public class CaptureTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    [Fact]
    public async Task Capture_Upload()
    {
        var uploadRequest = new CaptureUploadRequest
        {
            CaptureId = Guid.NewGuid()
        };
        var metaContent = JsonSerializer.Serialize(uploadRequest);
        var metaPart = new StreamPart(new MemoryStream(Encoding.UTF8.GetBytes(metaContent)), "meta.json",
            "application/json");

        var captureContent = "dummy capture file content";
        var capturePart = new StreamPart(new MemoryStream(Encoding.UTF8.GetBytes(captureContent)), "file.ccfcap",
            "application/octet-stream");

        var expected = new CaptureUploadResponse { CaptureId = Guid.NewGuid() };
        fixture.CaptureServiceMock
            .Setup(s => s.Upload(It.IsAny<Guid>(), It.IsAny<CaptureUploadRequest>(), It.IsAny<IFormFile>()))
            .ReturnsAsync(ApiResult<CaptureUploadResponse>.Success(expected));

        var result = await fixture.ApiClient.UploadCapture("unused", metaPart, capturePart);

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data.CaptureId.ShouldBe(expected.CaptureId);
    }
    
    [Fact]
    public async Task Capture_Delete()
    {
        var captureId = Guid.NewGuid();
        fixture.CaptureServiceMock
            .Setup(s => s.Delete(It.IsAny<Guid>(), captureId))
            .ReturnsAsync(ApiResult.Success());

        var result = await fixture.ApiClient.DeleteCapture("unused", captureId);

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
    }
    
    [Fact]
    public async Task Capture_List()
    {
        var expected = new CaptureListResponse { Captures = [new CaptureListElement { CaptureId = Guid.NewGuid() }] };
        fixture.CaptureServiceMock
            .Setup(s => s.GetCaptures(It.IsAny<Guid>()))
            .ReturnsAsync(ApiResult<CaptureListResponse>.Success(expected));

        var result = await fixture.ApiClient.GetCaptureList("unused");

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data.Captures.ShouldBeEquivalentTo(expected.Captures);
    }

}