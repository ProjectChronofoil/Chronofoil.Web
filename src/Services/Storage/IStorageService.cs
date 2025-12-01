using Amazon.S3.Model;

namespace Chronofoil.Web.Services.Storage;

public interface IStorageService
{
    Task<bool> UploadFileAsync(string fileName, Stream fileStream, Dictionary<string, string>? metadata = null);
    Task<bool> DeleteFileAsync(string fileName);
    Task<Stream?> GetFileAsync(string fileName);
    Task<MetadataCollection?> FileExistsAsync(string fileName);
    Task<List<string>> ListFilesAsync();
}