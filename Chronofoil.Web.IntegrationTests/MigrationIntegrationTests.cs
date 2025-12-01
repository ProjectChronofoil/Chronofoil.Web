using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Chronofoil.Web.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Testcontainers.LocalStack;
using Testcontainers.PostgreSql;
using Xunit;

namespace Chronofoil.Web.IntegrationTests;

[Trait("Category", "IntegrationTest")]
public class MigrationIntegrationTests : IClassFixture<ApiIntegrationTestFixture>
{
    private readonly ApiIntegrationTestFixture _fixture;
    private readonly Guid _testCaptureId = Guid.NewGuid();
    private readonly string _uploadDir;

    public MigrationIntegrationTests(ApiIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _uploadDir = Path.Combine(Path.GetTempPath(), "cf_migration_test_" + Guid.NewGuid());
    }

    [Fact]
    public async Task Test_Migration_Success()
    {
        // Setup local file
        Directory.CreateDirectory(_uploadDir);
        var filePath = Path.Combine(_uploadDir, $"{_testCaptureId}.ccfcap");
        await File.WriteAllTextAsync(filePath, "dummy content");

        // Seed database manually using the fixture's connection string
        // We create a new context to ensure data is committed before the app starts
        var connectionString = _fixture.DbConnectionString;
        var options = new DbContextOptionsBuilder<ChronofoilDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        using (var context = new ChronofoilDbContext(options, null!))
        {
            context.Uploads.Add(new ChronofoilUpload
            {
                CfCaptureId = _testCaptureId,
                UserId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                MetricTime = DateTime.UtcNow,
                MetricWhenEos = false,
                PublicTime = DateTime.UtcNow,
                PublicWhenEos = false
            });
            await context.SaveChangesAsync();
        }

        // Create a custom factory that uses the fixture's containers but adds our specific config
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<ChronofoilDbContext>>();
                    services.AddDbContext<ChronofoilDbContext>(options => { options.UseNpgsql(connectionString); });
                });

                builder.ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["JWT_SecretKey"] = "Large key used for integration tests :)",
                        ["JWT_Issuer"] = "cf_test",
                        ["UploadDirectory"] = _uploadDir,
                        ["MigrateStorage"] = "true",
                        ["S3_BucketName"] = "test-bucket",
                        ["S3_AccessKey"] = "test",
                        ["S3_SecretKey"] = "test",
                        ["S3_DisablePayloadSigning"] = "false",
                        ["S3_Endpoint"] = _fixture.S3ConnectionString,
                    }!);
                });
            });

        // Create the S3 bucket before triggering startup
        await _fixture.S3Client.PutBucketAsync(new PutBucketRequest { BucketName = "test-bucket" });

        // Trigger startup
        var client = factory.CreateClient();

        // Wait a bit for background service
        await Task.Delay(2000);

        // Verify file exists in S3 using the fixture's S3 client
        var request = new GetObjectRequest
        {
            BucketName = "test-bucket",
            Key = $"{_testCaptureId}.ccfcap"
        };

        var response = await _fixture.S3Client.GetObjectAsync(request);
        response.HttpStatusCode.ShouldBe(HttpStatusCode.OK);

        using var reader = new StreamReader(response.ResponseStream);
        var content = await reader.ReadToEndAsync();
        content.ShouldBe("dummy content");

        // Cleanup
        if (Directory.Exists(_uploadDir))
        {
            Directory.Delete(_uploadDir, true);
        }
        await factory.DisposeAsync();
    }
}
