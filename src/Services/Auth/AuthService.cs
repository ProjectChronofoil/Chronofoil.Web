using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Chronofoil.Common.Auth;
using Chronofoil.Web.Persistence;
using Chronofoil.Web.Services.Auth.External;
using Chronofoil.Web.Services.Database;
using Chronofoil.Web.Services.Info;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OneOf;

namespace Chronofoil.Web.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _log;
    private readonly CfDbService _db;
    private readonly InfoService _infoService;
    private readonly IServiceProvider _services;
    
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly TimeSpan _localTokenLifetime;

    public AuthService(
        IConfiguration config,
        ILogger<AuthService> log,
        CfDbService db,
        InfoService infoService,
        IServiceProvider services)
    {
        _log = log;
        _db = db;
        _infoService = infoService;
        _services = services;
        _config = config;
        
        _localTokenLifetime = TimeSpan.FromHours(Convert.ToDouble(_config["JWT_TokenLifetimeHours"]));
    }

    private async 
        Task<(AccessTokenResponse? response,
            IExternalAuthService.UserInfo? userInfo)>
        AuthUser(string provider, string authCode)
    {
        var service = _services.GetRequiredKeyedService<IExternalAuthService>(provider);
        
        AccessTokenResponse response;
        IExternalAuthService.UserInfo userInfo;
        try
        {
            response = await service.ExchangeCodeForTokenAsync(authCode);
            userInfo = await service.GetUserInfoAsync(response.AccessToken);
        }
        catch (AuthenticationException authEx)
        {
            _log.Log(LogLevel.Error, authEx, "Failed to refresh authentication with {provider}.", provider);
            return (null, null);
        }

        return (response, userInfo);
    }

    private async 
        Task<(AccessTokenResponse? response,
            IExternalAuthService.UserInfo? userInfo)>
        RefreshUser(string provider, string refreshCode)
    {
        var service = _services.GetRequiredKeyedService<IExternalAuthService>(provider);
        
        AccessTokenResponse response;
        IExternalAuthService.UserInfo userInfo;
        try
        {
            response = await service.ExchangeRefreshCodeForTokenAsync(refreshCode);
            userInfo = await service.GetUserInfoAsync(response.AccessToken);
        }
        catch (AuthenticationException authEx)
        {
            _log.Log(LogLevel.Error, authEx, "Failed to refresh authentication with {provider}.", provider);
            return (null, null);
        }

        return (response, userInfo);
    }

    // public async Task<IExternalAuthService.UserInfo> GetUserInfo(User user, string provider)
    // {
    //     var remoteToken = user.RemoteTokenInfos.FirstOrDefault(tok => tok.Provider == provider);
    //     if (remoteToken == null)
    //         throw new ArgumentException($"User does not have a remote token for {provider}.");
    //
    //     IExternalAuthService.UserInfo? userInfo;
    //     if (remoteToken.ExpiryTime < DateTime.UtcNow)
    //     {
    //         (var response, userInfo) = await RefreshUser(provider, remoteToken.RefreshToken);
    //         if (response == null || userInfo == null)
    //             throw new Exception("User refresh failed.");
    //
    //         var newRemoteToken = new RemoteTokenInfo(
    //             Guid.NewGuid(),
    //             user,
    //             remoteToken.CfTokens,
    //             provider,
    //             userInfo.UserId,
    //             userInfo.Username,
    //             response.AccessToken,
    //             response.RefreshToken,
    //             DateTime.UtcNow.AddSeconds(response.ExpiresIn));
    //         
    //         user.RemoteTokenInfos.Remove(remoteToken);
    //         user.RemoteTokenInfos.Add(newRemoteToken);
    //         await _db.Save();
    //     }
    //     else
    //     {
    //         var authProvider = _services.GetRequiredKeyedService<IExternalAuthService>(provider);
    //         userInfo = await authProvider.GetUserInfoAsync(remoteToken.AccessToken);
    //     }
    //
    //     return userInfo;
    // }

    private static string GenerateRefreshToken()
    {
        var refreshToken = RandomNumberGenerator.GetBytes(32);
        var refreshTokenString = string.Join("", refreshToken.Select(b => b.ToString("x2")));
        return refreshTokenString;
    }

    private string GenerateJwtToken(Guid cfUserId, string userName, string provider, TimeSpan tokenDuration)
    {
        var secretKey = _config["JWT_SecretKey"]!;
        var issuer = _config["JWT_Issuer"]!;
        
        var key = Encoding.ASCII.GetBytes(secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] 
            {
                new Claim(ClaimTypes.NameIdentifier, cfUserId.ToString()),
                new Claim(ClaimTypes.Name, userName),
                new Claim("AuthProvider", provider)
            }),
            Issuer = issuer,
            Expires = DateTime.UtcNow.Add(tokenDuration),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = _handler.CreateToken(tokenDescriptor);

        return _handler.WriteToken(token);
    }

    public async Task<OneOf<AccessTokenResponse, StatusCodeResult>> Register(string provider, string authCode)
    {
        var (response, userInfo) = await AuthUser(provider, authCode);
        if (response == null || userInfo == null) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        var existingUser = await _db.GetUser(provider, userInfo.UserId);
        if (existingUser != null) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        
        var user = new User(Guid.NewGuid(), _infoService.GetCurrentTos().Version, false, false);
        
        var remoteToken = new RemoteTokenInfo(
            Guid.NewGuid(),    
            user.CfUserId,
            provider,
            userInfo.UserId,
            userInfo.Username,
            response.AccessToken,
            response.RefreshToken,
            DateTime.UtcNow.AddSeconds(response.ExpiresIn));
        
        var jwtToken = GenerateJwtToken(user.CfUserId, userInfo.Username, provider, _localTokenLifetime);
        var refreshToken = GenerateRefreshToken();
        var localToken = new CfTokenInfo(Guid.NewGuid(), user.CfUserId, remoteToken.TokenId, refreshToken);
        
        await _db.AddUser(user);
        await _db.AddRemoteToken(remoteToken);
        await _db.AddCfToken(localToken);
        await _db.Save();
        
        return new AccessTokenResponse
        {
            AccessToken = jwtToken,
            RefreshToken = refreshToken,
            ExpiresIn = (long)_localTokenLifetime.TotalSeconds
        };
    }

    public async Task<OneOf<AccessTokenResponse, StatusCodeResult>> Login(string provider, string authCode)
    {
        var (response, userInfo) = await AuthUser(provider, authCode);
        if (response == null || userInfo == null) return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var existingRemoteTokenForProvider = await _db.GetRemoteToken(provider, userInfo.UserId);
        if (existingRemoteTokenForProvider == null) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        var existingUser = await _db.GetUser(existingRemoteTokenForProvider.UserId);
        if (existingUser == null) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        
        var remoteToken = new RemoteTokenInfo(
            Guid.NewGuid(),
            existingUser.CfUserId,
            provider,
            userInfo.UserId,
            userInfo.Username,
            response.AccessToken,
            response.RefreshToken,
            DateTime.UtcNow.AddSeconds(response.ExpiresIn));
        
        var jwtToken = GenerateJwtToken(existingUser.CfUserId, userInfo.Username, provider, _localTokenLifetime);
        var refreshToken = GenerateRefreshToken();
        var localToken = new CfTokenInfo(Guid.NewGuid(), existingUser.CfUserId, remoteToken.TokenId, refreshToken);

        await _db.ReplaceRemoteToken(remoteToken);
        await _db.AddCfToken(localToken);
        await _db.Save();
        
        return new AccessTokenResponse
        {
            AccessToken = jwtToken,
            RefreshToken = refreshToken,
            ExpiresIn = (long)_localTokenLifetime.TotalSeconds
        };
    }

    public async Task<OneOf<AccessTokenResponse, StatusCodeResult>> RefreshToken(string refreshToken)
    {
        var tokenSubLen = Math.Min(refreshToken.Length, 8);
        var tokenSub = refreshToken[..tokenSubLen];
        _log.LogInformation("Refreshing for {token}", tokenSub);
        var oldLocalToken = await _db.GetCfToken(refreshToken);
        if (oldLocalToken == null)
        {
            _log.LogError("oldLocalToken for {tokenSub} was null!", tokenSub);
            return new StatusCodeResult(StatusCodes.Status403Forbidden);
        }
        var oldRemoteToken = await _db.GetRemoteToken(oldLocalToken.RemoteTokenId);
        if (oldRemoteToken == null)
        {
            _log.LogError("oldRemoteToken for {tokenSub} was null!", tokenSub);
            return new StatusCodeResult(StatusCodes.Status403Forbidden);
        }
        var user = await _db.GetUser(oldLocalToken.UserId);
        if (user == null)
        {
            _log.LogError("User was null for {tokenSub}?", tokenSub);
            return new StatusCodeResult(StatusCodes.Status403Forbidden);
        }

        var (response, userInfo) = await RefreshUser(oldRemoteToken.Provider, oldRemoteToken.RefreshToken);
        if (response == null || userInfo == null) return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        
        var jwtToken = GenerateJwtToken(user.CfUserId, userInfo.Username, userInfo.Provider, _localTokenLifetime);
        var newRefreshToken = GenerateRefreshToken();
        
        var newRemoteToken = new RemoteTokenInfo(
            Guid.NewGuid(),
            user.CfUserId,
            userInfo.Provider,
            userInfo.UserId,
            userInfo.Username,
            response.AccessToken,
            response.RefreshToken,
            DateTime.UtcNow.AddSeconds(response.ExpiresIn));
        var newLocalToken = new CfTokenInfo(Guid.NewGuid(), user.CfUserId, newRemoteToken.TokenId, newRefreshToken);
        
        _db.DropCfToken(oldLocalToken);
        await _db.ReplaceRemoteToken(newRemoteToken);
        await _db.AddCfToken(newLocalToken);
        await _db.Save();

        return new AccessTokenResponse
        {
            AccessToken = jwtToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = (long)_localTokenLifetime.TotalSeconds
        };
    }

    public async Task<StatusCodeResult> AcceptTosVersion(Guid userId, int version)
    {
        var user = await _db.GetUser(userId);
        if (user == null) return new StatusCodeResult(StatusCodes.Status403Forbidden);

        user.TosVersion = version;
        await _db.Save();
        return new OkResult();
    }
}