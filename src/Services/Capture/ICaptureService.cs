using Chronofoil.Common;
using Chronofoil.Common.Capture;

namespace Chronofoil.Web.Services.Capture;

public interface ICaptureService
{
    Task<ApiResult<CaptureUploadResponse>> Upload(Guid userId, CaptureUploadRequest request, IFormFile file);
    Task<ApiResult> Delete(Guid userId, Guid captureId);
    Task<ApiResult<CaptureListResponse>> GetCaptures(Guid userId);
}