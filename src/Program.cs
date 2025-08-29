using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon.Runtime;
using Amazon.S3;
using Chronofoil.Web.Persistence;
using Chronofoil.Web.Services.Auth;
using Chronofoil.Web.Services.Auth.External;
using Chronofoil.Web.Services.Capture;
using Chronofoil.Web.Services.Censor;
using Chronofoil.Web.Services.Database;
using Chronofoil.Web.Services.Info;
using Chronofoil.Web.Services.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Serilog;
using Serilog.Context;

namespace Chronofoil.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));
        Serilog.Debugging.SelfLog.Enable(TextWriter.Synchronized(Console.Out));
        
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

                var secretKey = builder.Configuration.GetSection("JWT_SecretKey").Value!;
                secretKey = Regex.Unescape(secretKey);
                
                options.Authority = isDev ? "http://localhost:8080" : "https://cf.perchbird.dev";
                options.Audience = builder.Configuration["JWT_Audience"];
                options.ClaimsIssuer = builder.Configuration["JWT_Issuer"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.ASCII.GetBytes(secretKey)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                };
                if (builder.Environment.IsDevelopment())
                {
                    options.RequireHttpsMetadata = false;
                }
            });
        
        builder.Services.AddKeyedScoped<IExternalAuthService, DiscordExternalAuthService>("discord");
        builder.Services.AddDbContext<ChronofoilDbContext>();
        builder.Services.AddScoped<IDbService, CfDbService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<ICensorService, CensorService>();
        builder.Services.AddScoped<ICaptureService, CaptureService>();
        builder.Services.AddScoped<IInfoService, InfoService>();

        builder.Services.AddScoped<IAmazonS3>(provider =>
        {
            var config = provider.GetService<IConfiguration>()!;
            var s3Config = new AmazonS3Config
            {
                ServiceURL = config["S3_Endpoint"],
                ForcePathStyle = true,
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_SUPPORTED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_SUPPORTED
            };
            
            return new AmazonS3Client(
                config["S3_AccessKey"],
                config["S3_SecretKey"],
                s3Config);
        });
        builder.Services.AddScoped<IStorageService, S3StorageService>();
        
        if (builder.Environment.IsEnvironment("Staging") || builder.Environment.IsEnvironment("Production"))
        {
            builder.Services.AddMetricServer(options =>
            {
                options.Url = "/metrics";
                options.Port = 9184;
            });   
        }
        
        var app = builder.Build();
        
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        Migrate(app);

        app.Use(async (context, next) =>
        {
            var ua = context.Request.Headers.UserAgent;
            using var _ = LogContext.PushProperty("UserAgent", ua);
            await next.Invoke();
        });

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