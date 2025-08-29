using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Chronofoil.Common;
using Chronofoil.Web.Persistence;
using Chronofoil.Web.Services.Auth.External;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Refit;
using Testcontainers.LocalStack;
using Testcontainers.PostgreSql;
using Xunit;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Chronofoil.Web.IntegrationTests;

public class ApiIntegrationTestFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program> _webAppFactory;
    private PostgreSqlContainer _dbContainer;
    private LocalStackContainer _storageContainer;
    private HttpClient _httpClient;

    public RespawnWrapper Respawner { get; set; }
    public IChronofoilClient ApiClient { get; set; }
    public IAmazonS3 S3Client { get; set; }

    public async Task InitializeAsync()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithDatabase("cf_testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .Build();

        _storageContainer = new LocalStackBuilder().Build();
        
        await _dbContainer.StartAsync();
        await _storageContainer.StartAsync();

        _webAppFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var connectionString = _dbContainer.GetConnectionString();
                    services.RemoveAll<DbContextOptions<ChronofoilDbContext>>();
                    services.AddDbContext<ChronofoilDbContext>(options => { options.UseNpgsql(connectionString); });
                    
                    services.RemoveAll<IExternalAuthService>();
                    services.AddKeyedScoped<IExternalAuthService, MockExternalAuthService>("testProvider");
                });

                builder.ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["JWT_SecretKey"] = "Large key used for integration tests :)",
                        ["JWT_Issuer"] = "cf_test",
                        ["UploadDirectory"] = "upload_data",
                        ["S3_BucketName"] = "test-bucket",
                        ["S3_AccessKey"] = "test",
                        ["S3_SecretKey"] = "test",
                        ["S3_Endpoint"] = _storageContainer.GetConnectionString(),
                    }!);
                });
            });
        
        using var scope = _webAppFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChronofoilDbContext>();
        await dbContext.Database.MigrateAsync();
        
        S3Client = await GetS3Client();
        
        Respawner = await RespawnWrapper.CreateAsync(_dbContainer.GetConnectionString());

        var nullTask = Task.FromResult<Exception>(null!);
        var refitSettings = new RefitSettings
        {
            ExceptionFactory = _ => nullTask!,
        };
        _httpClient = _webAppFactory.CreateClient();
        
        ApiClient = RestService.For<IChronofoilClient>(_httpClient, refitSettings);
    }

    private async Task<AmazonS3Client> GetS3Client()
    {
        var s3Config = new AmazonS3Config
        {
            ServiceURL = _storageContainer.GetConnectionString(),
            ForcePathStyle = true,
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_SUPPORTED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_SUPPORTED
        };
            
        var s3Client = new AmazonS3Client(
            "test",
            "test",
            s3Config);
        await s3Client.PutBucketAsync(new PutBucketRequest { BucketName = "test-bucket" });
        
        return s3Client;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await _storageContainer.DisposeAsync();
        await _webAppFactory.DisposeAsync();
        _httpClient.Dispose();
    }
}