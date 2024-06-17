using Chronofoil.CaptureFile.Censor;
using Chronofoil.Common.Censor;
using Chronofoil.Web.Services.Database;

namespace Chronofoil.Web.Services.Censor;

public class CensorService
{
    // TODO: Modularize. Updating the service every time the game updates is not that bad rn
    private const string GameVersion = "2024.04.23.0000.0000";

    private ILogger<CensorService> _log;
    private CfDbService _db;

    private readonly string[] _validOpcodeKeys = Enum.GetValues<KnownCensoredOpcode>().Select(e => e.ToString()).ToArray();
    private int ValidOpcodeCount => _validOpcodeKeys.Length;

    public CensorService(ILogger<CensorService> log, CfDbService db)
    {
        _log = log;
        _db = db;
    }
    
    public async Task<bool> ProcessFoundOpcodes(Guid user, FoundOpcodesRequest opcode)
    {
        if (opcode.GameVersion != GameVersion)
        {
            _log.LogError("Received opcodes for wrong game version from {user}", user);
            _log.LogError("Expected: '{gv1}' Actual: '{gv2}'", GameVersion, opcode.GameVersion);
            return false;
        }

        if (opcode.Opcodes.Count > ValidOpcodeCount)
        {
            _log.LogError("Received too many opcodes to be a valid request from {user}", user);
            return false;
        }

        var validOpcodes = opcode.Opcodes.Where(x => _validOpcodeKeys.Contains(x.Key)).ToList();
        var invalidOpcodes = opcode.Opcodes.Except(validOpcodes).ToList();

        // The opcode count here to take is arbitrary - we just don't want to log 30,000 fake opcodes if that's sent to us
        foreach (var pair in invalidOpcodes.Take(ValidOpcodeCount))
        {
            _log.LogError("Received opcodes with invalid key '{key}' from {user}", pair.Key, user);
            return false;
        }

        foreach (var pair in validOpcodes)
        {
            var existingOpcode = await _db.FindCensorableOpcode(opcode.GameVersion, pair.Key);
            if (existingOpcode != null)
            {
                if (existingOpcode.Opcode != pair.Value)
                {
                    _log.LogError(
                        "'{existingKey}: {existingOpcode}' in DB, but received '{requestKey}: {requestOpcode}' from {user}",
                        existingOpcode.Key,
                        existingOpcode.Opcode,
                        pair.Key,
                        pair.Value,
                        user);
                    return false;   
                }
                continue;
            }

            await _db.AddCensorableOpcode(opcode.GameVersion, pair.Key, pair.Value);
        }
        await _db.Save();
        
        return true;
    }
    
    public async Task<CensoredOpcodesResponse> GetCurrentOpcodes(string gameVersion)
    {
        var result = await _db.GetOpcodes(gameVersion, _validOpcodeKeys);
        var ret = new CensoredOpcodesResponse
        {
            GameVersion = gameVersion,
            Opcodes = new Dictionary<string, int>()
        };

        foreach (var op in result)
        {
            ret.Opcodes[op.Key] = op.Opcode;
        }

        return ret;
    }
}