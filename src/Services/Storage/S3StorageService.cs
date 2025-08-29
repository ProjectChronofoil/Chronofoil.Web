using System.Net;
using Amazon.S3;
using Amazon.S3.Model;

namespace Chronofoil.Web.Services.Storage;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(IAmazonS3 s3Client, IConfiguration configuration, ILogger<S3StorageService> logger)
    {
        _s3Client = s3Client;
        _bucketName = configuration["S3_BucketName"]!;
        _logger = logger;
    }

    public async Task<bool> UploadFileAsync(string fileName, Stream fileStream, Dictionary<string, string>? metadata = null)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                InputStream = fileStream,
                ContentType = "application/octet-stream",
                DisableDefaultChecksumValidation = true
            };
            
            if (metadata != null)
            {
                foreach (var (key, val) in metadata)
                {
                    request.Metadata.Add(key, val);
                }
            }

            var response = await _s3Client.PutObjectAsync(request);
            
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Uploaded {fileName} to S3", fileName);
                return true;
            }

            _logger.LogError("Failed to upload file {fileName} to S3. Status: {status}", fileName, response.HttpStatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while uploading file {fileName} to S3", fileName);
            return false;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileName)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName
            };

            var response = await _s3Client.DeleteObjectAsync(request);
            
            if (response.HttpStatusCode == HttpStatusCode.NoContent)
            {
                _logger.LogInformation("Successfully deleted file {fileName} from S3", fileName);
                return true;
            }

            _logger.LogError("Failed to delete file {fileName} from S3. Status: {status}", fileName, response.HttpStatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while deleting file {fileName} from S3", fileName);
            return false;
        }
    }

    public async Task<Stream?> GetFileAsync(string fileName)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName
            };

            var response = await _s3Client.GetObjectAsync(request);
            
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Successfully retrieved file {fileName} from S3", fileName);
                return response.ResponseStream;
            }

            _logger.LogError("Failed to retrieve file {fileName} from S3. Status: {status}", fileName, response.HttpStatusCode);
            return null;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("File {fileName} not found in S3", fileName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving file {fileName} from S3", fileName);
            return null;
        }
    }

    public async Task<MetadataCollection?> FileExistsAsync(string fileName)
    {
        try
        {
            var request = new GetObjectMetadataRequest()
            {
                BucketName = _bucketName,
                Key = fileName
            };

            var response = await _s3Client.GetObjectMetadataAsync(request);
            
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                return response.Metadata;
            }

            _logger.LogError("Failed to retrieve file {fileName} from S3. Status: {status}", fileName, response.HttpStatusCode);
            return null;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("File {fileName} not found in S3", fileName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving file {fileName} from S3", fileName);
            return null;
        }
    }
}