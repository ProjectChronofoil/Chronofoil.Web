using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Chronofoil.Web.Persistence;
using Chronofoil.Web.Services.Auth;
using Chronofoil.Web.Services.Auth.External;
using Chronofoil.Web.Services.Capture;
using Chronofoil.Web.Services.Censor;
using Chronofoil.Web.Services.Database;
using Chronofoil.Web.Services.Info;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

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
        builder.Services.AddScoped<CfDbService, CfDbService>();
        builder.Services.AddScoped<AuthService, AuthService>();
        builder.Services.AddScoped<CensorService, CensorService>();
        builder.Services.AddScoped<CaptureService, CaptureService>();
        builder.Services.AddScoped<InfoService, InfoService>();

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