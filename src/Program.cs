using System.Text;
using System.Text.Json;
using Chronofoil.Web.Persistence;
using Chronofoil.Web.Services.Auth;
using Chronofoil.Web.Services.Auth.External;
using Chronofoil.Web.Services.Capture;
using Chronofoil.Web.Services.Censor;
using Chronofoil.Web.Services.Database;
using Chronofoil.Web.Services.Info;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Chronofoil.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddEnvironmentVariables();

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var isDev = builder.Environment.IsDevelopment();
                
                options.Authority = isDev ? "http://localhost:8080" : "https://cf.perchbird.dev";
                options.Audience = builder.Configuration["JWT:Audience"];
                options.ClaimsIssuer = builder.Configuration["JWT:Issuer"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.ASCII.GetBytes(builder.Configuration.GetSection("JWT:SecretKey").Value!)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false
                };
                if (builder.Environment.IsDevelopment())
                {
                    options.RequireHttpsMetadata = false;
                }
            });
        
        builder.Services.AddKeyedScoped<IExternalAuthService, DiscordExternalAuthService>("discord");
        builder.Services.AddDbContext<ChronofoilDbContext>();
        builder.Services.AddScoped<CfDbService, CfDbService>();
        builder.Services.AddScoped<AuthService, AuthService>();
        builder.Services.AddScoped<CensorService, CensorService>();
        builder.Services.AddScoped<CaptureService, CaptureService>();
        builder.Services.AddScoped<InfoService, InfoService>();
        // builder.Services.AddHttpLogging(o => o.LoggingFields = HttpLoggingFields.All);
        // builder.Services.AddLogging(logging =>
        // {
        //     logging.AddSimpleConsole(options =>
        //     {
        //         options.SingleLine = true;
        //         options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
        //     });
        // });

        var app = builder.Build();
        
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
            // app.UseHttpLogging();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        // app.MapGet("/hello-world", [Authorize]() => $"Hello {User}!");
        
        app.MapControllers();

        Migrate(app);

        app.Run();
    }

    private static void Migrate(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ChronofoilDbContext>();
        context.Database.Migrate();
        context.PostMigrate();
    }
}