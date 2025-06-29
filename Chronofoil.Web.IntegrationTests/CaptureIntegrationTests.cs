using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Chronofoil.Common;
using Chronofoil.Common.Auth;
using Chronofoil.Common.Capture;
using Refit;
using Shouldly;
using Xunit;

namespace Chronofoil.Web.IntegrationTests;

[Trait("Category", "IntegrationTest")]
public class CaptureIntegrationTests : IClassFixture<ApiIntegrationTestFixture>
{
    private readonly ApiIntegrationTestFixture _fixture;
    private readonly Guid _testCaptureId = Guid.Parse("c9cb00b9-c0f6-462c-a63f-01658de4c39d");

    public CaptureIntegrationTests(ApiIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _ = _fixture.Respawner.ResetAsync();
        File.Delete($"upload_data/{_testCaptureId}.ccfcap");
    }

    private async Task<string> GetAuthToken(string authCode)
    {
        var request = new AuthRequest { AuthorizationCode = authCode };
        var result = await _fixture.ApiClient.Register("testProvider", request);
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data.ShouldNotBeNull();
        return result.Data.AccessToken;
    }
    
    private byte[] GetTestCapture(bool censored)
    {
        var root = $"TestData/TestCapture-{_testCaptureId}";
        return File.ReadAllBytes(censored ? $"{root}.ccfcap" : $"{root}.cfcap");
    }
    
    private async Task<ApiResult<CaptureUploadResponse>> UploadTestCapture(string token, Guid captureId, CaptureUploadRequest request)
    {
        var metaJson = JsonSerializer.Serialize(request);
        var metaStream = new MemoryStream(Encoding.UTF8.GetBytes(metaJson));
        var metaPart = new StreamPart(metaStream, "meta.json", "application/json");

        var captureBytes = GetTestCapture(true);
        var captureStream = new MemoryStream(captureBytes);
        var capturePart = new StreamPart(captureStream, $"{captureId}.ccfcap", "application/octet-stream");

        var result = await _fixture.ApiClient.UploadCapture(token, metaPart, capturePart);
        return result;
    }

    [Fact]
    public async Task Test_Upload_Failure_CaptureNotValid_Basic()
    {
        var token = await GetAuthToken("good_auth_code");
        var tmpGuid = Guid.NewGuid();
        
        var request = new CaptureUploadRequest
        {
            CaptureId = tmpGuid,
            MetricTime = DateTime.UtcNow.AddDays(7),
            MetricWhenEos = false,
            PublicTime = DateTime.UtcNow.AddDays(14),
            PublicWhenEos = false
        };
        
        var result = await UploadTestCapture(token, tmpGuid, request);

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.CaptureNotValid);
    }
    
    [Fact]
    public async Task Test_Delete_Failure_NotFound()
    {
        var token = await GetAuthToken("good_auth_code");
        var result = await _fixture.ApiClient.DeleteCapture(token, Guid.NewGuid());

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.CaptureNotFound);
    }
    
    [Fact]
    public async Task Test_Delete_Failure_NotOwner()
    {
        var ownerToken = await GetAuthToken("good_auth_code");
        var otherUserToken = await GetAuthToken("good_auth_code2");
        
        var request = new CaptureUploadRequest
        {
            CaptureId = _testCaptureId,
            MetricTime = DateTime.UtcNow.AddDays(7),
            MetricWhenEos = false,
            PublicTime = DateTime.UtcNow.AddDays(14),
            PublicWhenEos = false
        };
        
        var uploadResult = await UploadTestCapture(ownerToken, _testCaptureId, request);
        
        uploadResult.ShouldNotBeNull();
        uploadResult.StatusCode.ShouldBe(ApiStatusCode.Success);
        uploadResult.Data.ShouldNotBeNull();
        uploadResult.Data.CaptureId.ShouldBe(_testCaptureId);

        var deleteResult = await _fixture.ApiClient.DeleteCapture(otherUserToken, _testCaptureId);

        deleteResult.ShouldNotBeNull();
        deleteResult.StatusCode.ShouldBe(ApiStatusCode.CaptureNotFoundForUser);
    }

    [Fact]
    public async Task Test_GetList_Success_Empty()
    {
        var token = await GetAuthToken("good_auth_code");
        var result = await _fixture.ApiClient.GetCaptureList(token);

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(ApiStatusCode.Success);
        result.Data.ShouldNotBeNull();
        result.Data.Captures.ShouldBeEmpty();
    }
    
    [Fact]
    public async Task Test_Upload_Success_And_GetList_Success()
    {
        var token = await GetAuthToken("good_auth_code");

        var request = new CaptureUploadRequest
        {
            CaptureId = _testCaptureId,
            MetricTime = DateTime.UtcNow.AddDays(7),
            MetricWhenEos = false,
            PublicTime = DateTime.UtcNow.AddDays(14),
            PublicWhenEos = false
        };
        
        var uploadResult = await UploadTestCapture(token, _testCaptureId, request);

        uploadResult.ShouldNotBeNull();
        uploadResult.StatusCode.ShouldBe(ApiStatusCode.Success);
        uploadResult.Data.ShouldNotBeNull();
        uploadResult.Data.CaptureId.ShouldBe(_testCaptureId);

        var listResult = await _fixture.ApiClient.GetCaptureList(token);
        listResult.ShouldNotBeNull();
        listResult.StatusCode.ShouldBe(ApiStatusCode.Success);
        listResult.Data.ShouldNotBeNull();
        listResult.Data.Captures.Count.ShouldBe(1);
        listResult.Data.Captures[0].CaptureId.ShouldBe(_testCaptureId);
    }

    [Fact]
    public async Task Test_Delete_Success()
    {
        var token = await GetAuthToken("good_auth_code");

        var request = new CaptureUploadRequest
        {
            CaptureId = _testCaptureId,
            MetricTime = DateTime.UtcNow.AddDays(7),
            MetricWhenEos = false,
            PublicTime = DateTime.UtcNow.AddDays(14),
            PublicWhenEos = false
        };
        
        var uploadResult = await UploadTestCapture(token, _testCaptureId, request);
        uploadResult.StatusCode.ShouldBe(ApiStatusCode.Success);

        var deleteResult = await _fixture.ApiClient.DeleteCapture(token, _testCaptureId);
        deleteResult.ShouldNotBeNull();
        deleteResult.StatusCode.ShouldBe(ApiStatusCode.Success);
        
        var listResult = await _fixture.ApiClient.GetCaptureList(token);
        listResult.Data!.Captures.ShouldBeEmpty();
    }
    
    [Fact]
    public async Task Test_Upload_Failure_CaptureExists()
    {
        var token = await GetAuthToken("good_auth_code");
        
        var request = new CaptureUploadRequest
        {
            CaptureId = _testCaptureId,
            MetricTime = DateTime.UtcNow.AddDays(7),
            MetricWhenEos = false,
            PublicTime = DateTime.UtcNow.AddDays(14),
            PublicWhenEos = false
        };

        var result1 = await UploadTestCapture(token, _testCaptureId, request);
        result1.StatusCode.ShouldBe(ApiStatusCode.Success);

        var result2 = await UploadTestCapture(token, _testCaptureId, request);
        result2.ShouldNotBeNull();
        result2.StatusCode.ShouldBe(ApiStatusCode.CaptureExists);
    }
}