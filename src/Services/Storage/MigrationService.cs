using Chronofoil.Web.Services.Database;
using Chronofoil.Web.Services.Storage;

namespace Chronofoil.Web.Services.Storage;

public class MigrationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MigrationService> _logger;
    private readonly IConfiguration _configuration;

    public MigrationService(IServiceProvider serviceProvider, ILogger<MigrationService> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting storage migration...");

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbService>();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var uploadDir = _configuration["UploadDirectory"];

        if (string.IsNullOrEmpty(uploadDir))
        {
            _logger.LogError("UploadDirectory is not configured. Cannot migrate.");
            return;
        }

        var uploads = await db.GetAllUploads();
        var count = 0;
        var total = uploads.Count;

        // Get list of all existing files in S3 once
        _logger.LogInformation("Fetching existing files from S3...");
        var existingFiles = await storage.ListFilesAsync();
        var existingFileSet = new HashSet<string>(existingFiles);
        _logger.LogInformation("Found {count} existing files in S3", existingFileSet.Count);

        foreach (var upload in uploads)
        {
            if (stoppingToken.IsCancellationRequested) break;

            var fileName = $"{upload.CfCaptureId}.ccfcap";

            // Check if already in S3
            if (existingFileSet.Contains(fileName))
            {
                continue;
            }

            var localPath = Path.Combine(uploadDir, fileName);
            if (!File.Exists(localPath))
            {
                _logger.LogWarning("File {fileName} not found locally at {localPath}, skipping.", fileName, localPath);
                continue;
            }

            try
            {
                await using var stream = File.OpenRead(localPath);
                var metadata = new Dictionary<string, string>
                {
                    ["capture_id"] = upload.CfCaptureId.ToString(),
                    ["capture_start_time"] = upload.StartTime.ToString(),
                    ["capture_end_time"] = upload.EndTime.ToString(),
                    ["metric_time"] = upload.MetricTime.ToString(),
                    ["metric_when_eos"] = upload.MetricWhenEos.ToString(),
                    ["public_time"] = upload.PublicTime.ToString(),
                    ["public_when_eos"] = upload.PublicWhenEos.ToString()
                };

                var success = await storage.UploadFileAsync(fileName, stream, metadata);
                if (success)
                {
                    count++;
                    _logger.LogInformation("Migrated {fileName} ({count}/{total})", fileName, count, total);
                }
                else
                {
                    _logger.LogError("Failed to upload {fileName}", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error migrating {fileName}", fileName);
            }
        }

        _logger.LogInformation("Storage migration completed. Migrated {count} files.", count);
    }
}
