using Chronofoil.Common;
using Chronofoil.Common.Auth;
using Chronofoil.Web.Persistence;
using Chronofoil.Web.Services.Auth.External;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Refit;
using Respawn;
using Testcontainers.PostgreSql;
using Xunit;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Chronofoil.Web.IntegrationTests;

public class ApiIntegrationTestFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program> _webAppFactory;
    private PostgreSqlContainer _dbContainer;
    private HttpClient _httpClient;

    public RespawnWrapper Respawner { get; set; }
    public IChronofoilClient ApiClient { get; set; }

    public async Task InitializeAsync()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithDatabase("cf_testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .Build();
        
        await _dbContainer.StartAsync();

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
                    }!);
                });
            });
        
        using var scope = _webAppFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChronofoilDbContext>();
        await dbContext.Database.MigrateAsync();
        
        Respawner = await RespawnWrapper.CreateAsync(_dbContainer.GetConnectionString());

        var nullTask = Task.FromResult<Exception>(null!);
        var refitSettings = new RefitSettings
        {
            ExceptionFactory = _ => nullTask!,
        };
        _httpClient = _webAppFactory.CreateClient();
        
        ApiClient = RestService.For<IChronofoilClient>(_httpClient, refitSettings);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await _webAppFactory.DisposeAsync();
        _httpClient.Dispose();

    }
}