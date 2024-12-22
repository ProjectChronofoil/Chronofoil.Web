using System.Net.Http.Headers;
using System.Text.Json;
using Chronofoil.Common.Auth;
using Microsoft.AspNetCore.Authentication;

namespace Chronofoil.Web.Services.Auth.External;

public class DiscordExternalAuthService : IExternalAuthService
{
    private record DiscordUser(
        string Id,
        string Username,
        string Discriminator,
        string? Avatar,
        bool Verified,
        int Flags,
        string? Banner,
        int? AccentColor,
        int PremiumType,
        int PublicFlags);
    
    private const string ApiEndpoint = "https://discord.com/api/v10/oauth2/token";
    private const string RedirectUri = "http://localhost:43595/auth/login/discord";
    
    private readonly HttpClient _httpClient = new();
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly ILogger _log;

    public DiscordExternalAuthService(IConfiguration config, ILogger<DiscordExternalAuthService> log)
    {
        _clientId = config["Discord_ClientId"]!;
        _clientSecret = config["Discord_ClientSecret"]!;
        _log = log;
    }

    public async Task<AccessTokenResponse> ExchangeCodeForTokenAsync(string code)
    {
        var requestData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", RedirectUri),
        });
        
        var response = await _httpClient.PostAsync(ApiEndpoint, requestData);

        if (!response.IsSuccessStatusCode)
        {
            var contentString = await response.Content.ReadAsStringAsync();
            throw new AuthenticationFailureException($"Failed to authenticate: Response content: {{{contentString}}} Response: {response}");
        }
        
        var responseStr = await response.Content.ReadAsStringAsync();
        _log.LogInformation("[ExchangeCodeForTokenAsync] Response: {str}", responseStr);
        var responseObject = JsonSerializer.Deserialize<AccessTokenResponse>(responseStr, _serializerOptions);
        
        if (responseObject == null)
        {
            throw new Exception("Failed to deserialize access token response.");
        }
        
        return responseObject;
    }
    
    public async Task<AccessTokenResponse> ExchangeRefreshCodeForTokenAsync(string code)
    {
        var requestData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", code)
        });
        
        var response = await _httpClient.PostAsync(ApiEndpoint, requestData);

        if (!response.IsSuccessStatusCode)
        {
            var contentString = await response.Content.ReadAsStringAsync();
            throw new AuthenticationFailureException($"Failed to authenticate: Response content: {{{contentString}}} Response: {response}");
        }
        
        var responseStr = await response.Content.ReadAsStringAsync();
        _log.LogInformation("[ExchangeCodeForTokenAsync] Response: {str}", responseStr);
        var responseObject = JsonSerializer.Deserialize<AccessTokenResponse>(responseStr, _serializerOptions);
        
        if (responseObject == null)
        {
            throw new Exception("Failed to deserialize access token response.");
        }
        
        return responseObject;
    }
    
    public async Task<IExternalAuthService.UserInfo> GetUserInfoAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/v10/users/@me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var responseStr = await response.Content.ReadAsStringAsync();
        _log.LogInformation("[GetUserInfoAsync] Response: {str}", responseStr);
        var responseObject = JsonSerializer.Deserialize<DiscordUser>(responseStr, _serializerOptions);

        if (responseObject == null)
        {
            throw new Exception("Failed to deserialize user info response.");
        }

        return new IExternalAuthService.UserInfo("discord", responseObject.Username, responseObject.Id);
    }
    
}