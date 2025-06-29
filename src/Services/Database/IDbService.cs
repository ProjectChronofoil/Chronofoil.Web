using Chronofoil.Web.Persistence;

namespace Chronofoil.Web.Services.Database;

public interface IDbService
{
    public Task Save();
    
    public Task AddUser(User user);
    public Task<User?> GetUser(Guid cfUserId);
    public Task<User?> GetUser(string provider, string userId);
    public void UpdateUser(User user);

    public Task AddCfToken(CfTokenInfo token);
    public void DropCfToken(CfTokenInfo token);
    public Task<CfTokenInfo?> GetCfToken(string refreshToken);
    public Task<ICollection<CfTokenInfo>> GetCfTokens(Guid remoteTokenId);

    public Task AddRemoteToken(RemoteTokenInfo token);
    public void DropRemoteToken(RemoteTokenInfo token);
    public Task ReplaceRemoteToken(RemoteTokenInfo newRemoteToken);
    public Task<RemoteTokenInfo?> GetRemoteToken(Guid guid);
    public Task<RemoteTokenInfo?> GetRemoteToken(string provider, string providerUserId);

    public Task AddUpload(ChronofoilUpload upload);
    public void RemoveUpload(ChronofoilUpload upload);
    public Task<List<ChronofoilUpload>> GetUploads(Guid userId);
    public Task<ChronofoilUpload?> GetUploadById(Guid id);

    public Task<CensoredOpcode?> FindCensorableOpcode(string gameVersion, string key);
    public Task AddCensorableOpcode(string gameVersion, string key, int value);
    public Task<List<CensoredOpcode>> GetOpcodes(string gameVersion, string[] keys);
}