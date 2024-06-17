using Chronofoil.Common.Censor;
using Chronofoil.Web.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chronofoil.Web.Services.Database;

public class CfDbService : IDbService
{
    private readonly ILogger<CfDbService> _log;
    private readonly ChronofoilDbContext _db;

    public CfDbService(ILogger<CfDbService> log, ChronofoilDbContext db)
    {
        _log = log;
        _db = db;
    }

    public async Task Save()
    {
        var updates = await _db.SaveChangesAsync();
        _log.LogInformation("Saved {updates} changes", updates);
    }

    public async Task AddUser(User user)
    {
        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.CfUserId == user.CfUserId);
        if (existingUser != null)
        {
            _log.LogInformation("User already exists.");
            return;
        }
        
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        _log.LogInformation("User {id} added successfully.", user.CfUserId);
    }

    public async Task AddCfToken(CfTokenInfo token)
    {
        await _db.CfTokens.AddAsync(token);
    }
    
    public async Task AddRemoteToken(RemoteTokenInfo token)
    {
        await _db.RemoteTokens.AddAsync(token);
    }

    public async Task<User?> GetUser(Guid cfUserId)
    {
        return await _db.Users.FindAsync(cfUserId);
    }

    public void UpdateUser(User user)
    {
        _db.Users.Update(user);
    }

    public async Task<User?> GetUser(string provider, string userId)
    {
        var token = await GetRemoteToken(provider, userId);

        if (token == null) return null;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.CfUserId == token.UserId);
        return user;
    }

    public async Task<CfTokenInfo?> GetCfToken(string refreshToken)
    {
        var token = await _db
            .CfTokens
            .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken);
        
        return token;
    }

    public async Task<ICollection<CfTokenInfo>> GetCfTokens(Guid remoteTokenId)
    {
        var tokens = await _db
            .CfTokens
            .Where(t => t.RemoteTokenId == remoteTokenId)
            .ToListAsync();
        return tokens;
    }
    
    public async Task<RemoteTokenInfo?> GetRemoteToken(Guid guid)
    {
        var token = await _db
            .RemoteTokens
            .FirstOrDefaultAsync(tok => tok.TokenId == guid);
        return token;
    }

    public async Task<RemoteTokenInfo?> GetRemoteToken(string provider, string providerUserId)
    {
        var token = await _db
            .RemoteTokens
            .FirstOrDefaultAsync(tok => tok.Provider == provider && tok.ProviderUserId == providerUserId);
        return token;
    }

    public async Task ReplaceRemoteToken(RemoteTokenInfo newRemoteToken)
    {
        var oldRemoteToken = await GetRemoteToken(newRemoteToken.Provider, newRemoteToken.ProviderUserId);
        if (oldRemoteToken == null) return;
        
        var existingTokens = await GetCfTokens(oldRemoteToken.TokenId);
        foreach (var existingToken in existingTokens)
            existingToken.RemoteTokenId = newRemoteToken.TokenId;

        _db.RemoteTokens.Remove(oldRemoteToken);
        _db.RemoteTokens.Add(newRemoteToken);
    }

    public async Task<ChronofoilUpload?> GetUploadById(Guid id)
    {
        return await _db.Uploads.FirstOrDefaultAsync(u => u.CfCaptureId == id);
    }

    public async Task<List<ChronofoilUpload>> GetUploads(Guid userId)
    {
        return await _db.Uploads.Where(u => u.UserId == userId).ToListAsync();
    }

    public async Task AddUpload(ChronofoilUpload upload)
    {
        await _db.Uploads.AddAsync(upload);
    }
    
    public void RemoveUpload(ChronofoilUpload upload)
    {
        _db.Uploads.Remove(upload);
    }

    public void DropCfToken(CfTokenInfo token)
    {
        _db.CfTokens.Remove(token);
    }

    public void DropRemoteToken(RemoteTokenInfo token)
    {
        _db.RemoteTokens.Remove(token);
    }

    // public async Task<CensoredOpcode?> FindCensorableOpcode(FoundOpcodeRequest opcode)
    // {
    //     return await _db.Opcodes.FirstOrDefaultAsync(x => x.GameVersion == opcode.GameVersion && x.Key == opcode.Key);
    // }

    public async Task<CensoredOpcode?> FindCensorableOpcode(string gameVersion, string key)
    {
        return await _db.Opcodes.FirstOrDefaultAsync(x => x.GameVersion == gameVersion && x.Key == key);
    }

    // public async Task AddCensorableOpcode(FoundOpcodeRequest opcode)
    // {
    //     await _db.Opcodes.AddAsync(new CensoredOpcode { GameVersion = opcode.GameVersion, Key = opcode.Key, Opcode = opcode.Opcode });
    // }
    
    public async Task AddCensorableOpcode(string gameVersion, string key, int value)
    {
        await _db.Opcodes.AddAsync(new CensoredOpcode { GameVersion = gameVersion, Key = key, Opcode = value });
    }

    public async Task<List<CensoredOpcode>> GetOpcodes(string gameVersion, string[] keys)
    {
        var result = await _db.Opcodes.Where(x => x.GameVersion == gameVersion && keys.Contains(x.Key)).ToListAsync();
        return result;
    }
}