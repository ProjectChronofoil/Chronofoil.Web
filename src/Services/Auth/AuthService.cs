using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Chronofoil.Common;
using Chronofoil.Common.Auth;
using Chronofoil.Web.Persistence;
using Chronofoil.Web.Services.Auth.External;
using Chronofoil.Web.Services.Database;
using Chronofoil.Web.Services.Info;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Chronofoil.Web.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _log;
    private readonly IDbService _db;
    private readonly IInfoService _infoService;
    private readonly IServiceProvider _services;
    
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly TimeSpan _localTokenLifetime;

    public AuthService(
        IConfiguration config,
        ILogger<AuthService> log,
        IDbService db,
        IInfoService infoService,
        IServiceProvider services)
    {
        _log = log;
        _db = db;
        _infoService = infoService;
        _services = services;
        _config = config;
        
        _localTokenLifetime = TimeSpan.FromHours(Convert.ToDouble(_config["JWT_TokenLifetimeHours"]));
    }

    public async Task<(AccessTokenResponse? response, IExternalAuthService.UserInfo? userInfo)>
        AuthUser(string provider, string authCode)
    {
        var service = _services.GetRequiredKeyedService<IExternalAuthService>(provider);
        
        AccessTokenResponse? response;
        IExternalAuthService.UserInfo? userInfo = null;
        try
        {
            response = await service.ExchangeCodeForTokenAsync(authCode);
            
            if (response != null)
                userInfo = await service.GetUserInfoAsync(response.AccessToken);
        }
        catch (AuthenticationException authEx)
        {
            _log.Log(LogLevel.Error, authEx, "Failed to refresh authentication with {provider}.", provider);
            return (null, null);
        }

        return (response, userInfo);
    }

    public async Task<(AccessTokenResponse? response, IExternalAuthService.UserInfo? userInfo)>
        RefreshUser(string provider, string refreshCode)
    {
        var service = _services.GetRequiredKeyedService<IExternalAuthService>(provider);
        
        AccessTokenResponse? response;
        IExternalAuthService.UserInfo? userInfo = null;
        try
        {
            response = await service.ExchangeRefreshCodeForTokenAsync(refreshCode);
            
            if (response != null)
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

    public string GenerateRefreshToken()
    {
        var refreshToken = RandomNumberGenerator.GetBytes(32);
        var refreshTokenString = string.Join("", refreshToken.Select(b => b.ToString("x2")));
        return refreshTokenString;
    }

    public string GenerateJwtToken(Guid cfUserId, string userName, string provider, TimeSpan tokenDuration)
    {
        var secretKey = _config["JWT_SecretKey"]!;
        secretKey = Regex.Unescape(secretKey);
        var issuer = _config["JWT_Issuer"]!;
        
        var key = Encoding.ASCII.GetBytes(secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, cfUserId.ToString()),
                new Claim(ClaimTypes.Name, userName),
                new Claim("AuthProvider", provider)
            ]),
            Issuer = issuer,
            Expires = DateTime.UtcNow.Add(tokenDuration),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = _handler.CreateToken(tokenDescriptor);

        return _handler.WriteToken(token);
    }

    public async Task<ApiResult<AccessTokenResponse>> Register(string provider, string authCode)
    {
        var (response, userInfo) = await AuthUser(provider, authCode);
        if (response == null || userInfo == null)
            return ApiResult<AccessTokenResponse>.Failure(ApiStatusCode.AuthProviderAuthFailure);
        
        var existingUser = await _db.GetUser(provider, userInfo.UserId);
        if (existingUser != null)
            return ApiResult<AccessTokenResponse>.Failure(ApiStatusCode.UserExists);
        
        var user = new User(Guid.NewGuid(), _infoService.GetCurrentTos().Data!.Version, false, false);
        
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
        
        return ApiResult<AccessTokenResponse>.Success(new AccessTokenResponse
        {
            AccessToken = jwtToken,
            RefreshToken = refreshToken,
            ExpiresIn = (long)_localTokenLifetime.TotalSeconds
        });
    }

    public async Task<ApiResult<AccessTokenResponse>> Login(string provider, string authCode)
    {
        var (response, userInfo) = await AuthUser(provider, authCode);
        if (response == null || userInfo == null) 
            return ApiResult<AccessTokenResponse>.Failure(ApiStatusCode.AuthProviderAuthFailure);

        var existingRemoteTokenForProvider = await _db.GetRemoteToken(provider, userInfo.UserId);
        if (existingRemoteTokenForProvider == null)
            return ApiResult<AccessTokenResponse>.Failure(ApiStatusCode.UserRemoteTokenNotFound);
        
        var existingUser = await _db.GetUser(existingRemoteTokenForProvider.UserId);
        if (existingUser == null)
            return ApiResult<AccessTokenResponse>.Failure(ApiStatusCode.UserNotFound);
        
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

        return ApiResult<AccessTokenResponse>.Success(new AccessTokenResponse
        {
            AccessToken = jwtToken,
            RefreshToken = refreshToken,
            ExpiresIn = (long)_localTokenLifetime.TotalSeconds
        });
    }

    public async Task<ApiResult<AccessTokenResponse>> RefreshToken(string refreshToken)
    {
        var tokenSubLen = Math.Min(refreshToken.Length, 8);
        var tokenSub = refreshToken[..tokenSubLen];
        _log.LogInformation("Refreshing for {token}", tokenSub);
        var oldLocalToken = await _db.GetCfToken(refreshToken);
        if (oldLocalToken == null)
        {
            _log.LogError("oldLocalToken for {tokenSub} was null!", tokenSub);
            return ApiResult<AccessTokenResponse>.Failure(ApiStatusCode.UserTokenNotFound);
        }
        var oldRemoteToken = await _db.GetRemoteToken(oldLocalToken.RemoteTokenId);
        if (oldRemoteToken == null)
        {
            _log.LogError("oldRemoteToken for {tokenSub} was null!", tokenSub);
            return ApiResult<AccessTokenResponse>.Failure(ApiStatusCode.UserRemoteTokenNotFound);
        }
        var user = await _db.GetUser(oldLocalToken.UserId);
        if (user == null)
        {
            _log.LogError("User was null for {tokenSub}?", tokenSub);
            return ApiResult<AccessTokenResponse>.Failure(ApiStatusCode.UserNotFound);
        }

        var (response, userInfo) = await RefreshUser(oldRemoteToken.Provider, oldRemoteToken.RefreshToken);
        if (response == null || userInfo == null)
            return ApiResult<AccessTokenResponse>.Failure(ApiStatusCode.AuthProviderRefreshFailure);
        
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

        return ApiResult<AccessTokenResponse>.Success(new AccessTokenResponse
        {
            AccessToken = jwtToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = (long)_localTokenLifetime.TotalSeconds
        });
    }

    public async Task<ApiResult> AcceptTosVersion(Guid userId, int version)
    {
        var currentTosVersion = _infoService.GetCurrentTos().Data!.Version;
        if (version != currentTosVersion) return ApiResult.Failure(ApiStatusCode.AuthInvalidTosVersion);
        
        var user = await _db.GetUser(userId);
        if (user == null) return ApiResult.Failure(ApiStatusCode.UserNotFound);

        user.TosVersion = version;
        await _db.Save();
        return ApiResult.Success();
    }
}