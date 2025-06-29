using Chronofoil.Common;
using Chronofoil.Common.Censor;

namespace Chronofoil.Web.Services.Censor;

public interface ICensorService
{
    Task<ApiResult> ProcessFoundOpcodes(Guid user, FoundOpcodesRequest opcode);
    Task<ApiResult<CensoredOpcodesResponse>> GetCurrentOpcodes(string gameVersion);
}