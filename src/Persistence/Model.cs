// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Chronofoil.Web.Persistence;

public record User
{
    public Guid CfUserId { get; init; }
    public int TosVersion { get; set; }
    public bool IsAdmin { get; init; }
    public bool IsBanned { get; init; }

    public User() {}
    
    public User(
        Guid cfUserId,
        int tosVersion,
        bool isAdmin,
        bool isBanned)
    {
        CfUserId = cfUserId;
        TosVersion = tosVersion;
        IsAdmin = isAdmin;
        IsBanned = isBanned;
    }
}

public class CfTokenInfo
{
    public Guid TokenId { get; init; }
    public Guid UserId { get; init; }
    public Guid RemoteTokenId { get; set; }
    public string RefreshToken { get; init; }
    
    public CfTokenInfo() {}

    public CfTokenInfo(Guid tokenId, Guid user, Guid remoteToken, string refreshToken)
    {
        TokenId = tokenId;
        UserId = user;
        RemoteTokenId = remoteToken;
        RefreshToken = refreshToken;
    }
}

public class RemoteTokenInfo
{
    public Guid TokenId { get; init; }
    public Guid UserId { get; init; }
    public string Provider { get; init; }
    public string ProviderUserId { get; init; }
    public string Username { get; init; }
    public string AccessToken { get; init; }
    public string RefreshToken { get; init; }
    public DateTime ExpiryTime { get; init; }

    public RemoteTokenInfo() {}
    
    public RemoteTokenInfo(
        Guid tokenId,
        Guid user,
        string provider,
        string userId,
        string username,
        string accessToken,
        string refreshToken,
        DateTime expiryTime)
    {
        TokenId = tokenId;
        UserId = user;
        Provider = provider;
        ProviderUserId = userId;
        Username = username;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiryTime = expiryTime;
    }
}

public class ChronofoilUpload
{
    public Guid CfCaptureId { get; init; }
    public Guid UserId { get; init; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime MetricTime { get; init; }
    public bool MetricWhenEos { get; init; }
    public DateTime PublicTime { get; init; }
    public bool PublicWhenEos { get; init; }

    public ChronofoilUpload() {}

    public ChronofoilUpload(
        Guid cfCaptureId,
        Guid user,
        DateTime startTime,
        DateTime endTime,
        DateTime metricTime,
        bool metricWhenEos,
        DateTime publicTime,
        bool publicWhenEos)
    {
        CfCaptureId = cfCaptureId;
        UserId = user;
        StartTime = startTime;
        EndTime = endTime;
        MetricTime = metricTime;
        MetricWhenEos = metricWhenEos;
        PublicTime = publicTime;
        PublicWhenEos = publicWhenEos;
    }
}

public class CensoredOpcode
{
    public string GameVersion { get; init; }
    public string Key { get; init; }
    public int Opcode { get; init; }
}

// public record PendingChronofoilUpload(
//     [Required] Guid CfUserId,
//     [Required] Guid CfCaptureId
//     
// );