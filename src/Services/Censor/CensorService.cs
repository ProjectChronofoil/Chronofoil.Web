using Chronofoil.CaptureFile.Censor;
using Chronofoil.Common;
using Chronofoil.Common.Censor;
using Chronofoil.Web.Services.Database;

namespace Chronofoil.Web.Services.Censor;

public class CensorService : ICensorService
{
    // TODO: Modularize. Updating the service every time the game updates is not that bad rn
    private readonly HashSet<string> _gameVersions = [
        "2024.04.23.0000.0000",
        "2024.06.18.0000.0000",
        "2024.07.06.0000.0000",
        "2024.07.10.0001.0000",
        "2024.07.24.0000.0000",
        "2024.08.02.0000.0000",
        "2024.08.21.0000.0000",
        "2024.11.06.0000.0000",
        "2024.11.20.0000.0000",
        "2024.12.07.0000.0000",
        "2025.01.14.0000.0000",
        "2025.01.28.0000.0000",
        "2025.02.19.0000.0000",
        "2025.02.27.0000.0000",
        "2025.03.18.0000.0000",
        "2025.03.27.0000.0000",
        "2025.04.16.0000.0000",
        "2025.05.17.0000.0000",
        "2025.06.10.0000.0000",
        "2025.06.19.0000.0000",
        "2025.06.28.0000.0000",
        "2025.07.30.0000.0000",
        "2025.08.07.0000.0000",
        "2025.08.22.0000.0000",
        "2025.09.04.0000.0000",
    ];

    private readonly ILogger<CensorService> _log;
    private readonly IDbService _db;

    private readonly string[] _validOpcodeKeys = Enum.GetValues<KnownCensoredOpcode>().Select(e => e.ToString()).ToArray();
    private int ValidOpcodeCount => _validOpcodeKeys.Length;

    public CensorService(ILogger<CensorService> log, IDbService db)
    {
        _log = log;
        _db = db;
    }
    
    public async Task<ApiResult> ProcessFoundOpcodes(Guid user, FoundOpcodesRequest opcode)
    {
        if (!_gameVersions.Contains(opcode.GameVersion))
        {
            _log.LogError("Received opcodes for invalid game version from {user}", user);
            _log.LogError("Current: '{gv1}' Actual: '{gv2}'", _gameVersions.Last(), opcode.GameVersion);
            return ApiResult.Failure(ApiStatusCode.OpcodeUnknownGameVersion);
        }

        if (opcode.Opcodes.Count > ValidOpcodeCount)
        {
            _log.LogError("Received too many opcodes to be a valid request from {user}", user);
            return ApiResult.Failure(ApiStatusCode.OpcodeCountInvalid);
        }

        var validOpcodes = opcode.Opcodes.Where(x => _validOpcodeKeys.Contains(x.Key)).ToList();
        var invalidOpcodes = opcode.Opcodes.Except(validOpcodes).ToList();

        // The opcode count here to take is arbitrary - we just don't want to log 30,000 fake opcodes if that's sent to us
        foreach (var pair in invalidOpcodes.Take(ValidOpcodeCount))
        {
            _log.LogError("Received opcodes with invalid key '{key}' from {user}", pair.Key, user);
            return ApiResult.Failure(ApiStatusCode.OpcodeInvalidKey);
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
                    return ApiResult.Failure(ApiStatusCode.OpcodeMismatchWithKnown);
                }
                continue;
            }

            await _db.AddCensorableOpcode(opcode.GameVersion, pair.Key, pair.Value);
        }
        await _db.Save();
        
        return ApiResult.Success();
    }
    
    public async Task<ApiResult<CensoredOpcodesResponse>> GetCurrentOpcodes(string gameVersion)
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

        return ApiResult<CensoredOpcodesResponse>.Success(ret);
    }
}