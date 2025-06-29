using System.Security.Claims;
using System.Text.Encodings.Web;
using Chronofoil.Common;
using Chronofoil.Web.Persistence;
using Chronofoil.Web.Services.Auth;
using Chronofoil.Web.Services.Auth.External;
using Chronofoil.Web.Services.Capture;
using Chronofoil.Web.Services.Censor;
using Chronofoil.Web.Services.Database;
using Chronofoil.Web.Services.Info;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Refit;

namespace Chronofoil.Web.Tests;

public class ApiTestFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    public WebApplicationFactory<Program> WebAppFactory { get; }
    public IChronofoilClient ApiClient { get; }
    public Mock<IAuthService> AuthServiceMock { get; } = new();
    public Mock<ICensorService> CensorServiceMock { get; } = new();
    public Mock<ICaptureService> CaptureServiceMock { get; } = new();
    public Mock<IInfoService> InfoServiceMock { get; } = new();
    public Mock<IExternalAuthService> ExternalAuthServiceMock { get; } = new();
    public Mock<IDbService> DbServiceMock { get; } = new();

    public ApiTestFixture()
    {
        // We only need this db to exist so that ASP.NET can boot
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        WebAppFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<ChronofoilDbContext>>();
                    services.RemoveAll<ChronofoilDbContext>();

                    services.AddDbContext<ChronofoilDbContext>(options =>
                    {
                        // We're using pgsql migrations and sqlite provider, so ignore this error/warning
                        options.ConfigureWarnings(w =>
                            w.Ignore(RelationalEventId.PendingModelChangesWarning));
                        options.UseSqlite(_connection);
                    });

                    services.RemoveAll<IAuthService>();
                    services.RemoveAll<ICensorService>();
                    services.RemoveAll<ICaptureService>();
                    services.RemoveAll<IInfoService>();
                    services.RemoveAll<IExternalAuthService>();
                    services.RemoveAll<IDbService>();

                    services.AddSingleton(AuthServiceMock.Object);
                    services.AddSingleton(CensorServiceMock.Object);
                    services.AddSingleton(CaptureServiceMock.Object);
                    services.AddSingleton(InfoServiceMock.Object);
                    services.AddSingleton(ExternalAuthServiceMock.Object);
                    services.AddSingleton(DbServiceMock.Object);

                    services
                        .AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
                    services.AddAuthorizationBuilder()
                        .SetDefaultPolicy(new AuthorizationPolicyBuilder("Test")
                            .RequireAuthenticatedUser()
                            .Build());
                });
            });

        var httpClient = WebAppFactory.CreateClient();

        var nullTask = Task.FromResult<Exception>(null!);
        ApiClient = RestService.For<IChronofoilClient>(httpClient, new RefitSettings
        {
            ExceptionFactory = _ => nullTask!
        });
    }

    public void Dispose()
    {
        _connection.Close();
        WebAppFactory.Dispose();
        GC.SuppressFinalize(this);
    }
}

file class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "testUser"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
