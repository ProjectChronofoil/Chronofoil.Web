using Chronofoil.Common;
using Chronofoil.Common.Info;

namespace Chronofoil.Web.Services.Info;

public interface IInfoService
{
    ApiResult<FaqResponse> GetCurrentFaq();
    ApiResult<TosResponse> GetCurrentTos();
}