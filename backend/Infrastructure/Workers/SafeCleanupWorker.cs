using backend.Application.Interfaces;
using backend.Domain.Entities;
using backend.Infrastructure.Persistence;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace backend.Infrastructure.Workers;

public class SafeCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<SafeCleanupWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Chạy 1 tiếng 1 lần

    public SafeCleanupWorker(IServiceProvider serviceProvider, IConnectionMultiplexer redis, ILogger<SafeCleanupWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredVersionsAsync(stoppingToken);
                await CleanupTrashFilesAsync(stoppingToken); // Phase 5: Hard Delete Trash
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing SafeCleanupWorker.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CleanupTrashFilesAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SafeCleanupWorker: Checking for old trash files...");
        using var scope = _serviceProvider.CreateScope();
        var fileRepo = scope.ServiceProvider.GetRequiredService<IFileRepository>();
        
        // Scan for trash files (Simplified: In a large system, this should be paginated per workspace)
        var trashFiles = await fileRepo.GetTrashFilesAsync(Guid.Empty, 1000, 0); 
        
        foreach (var file in trashFiles.Where(f => f.DeletedAt < DateTime.UtcNow.AddDays(-30)))
        {
            if (stoppingToken.IsCancellationRequested) break;
            
            _logger.LogInformation($"SafeCleanupWorker: Hard deleting file {file.Id} from trash...");
            
            var versions = await fileRepo.GetVersionsByFileIdAsync(file.Id, 1000, 0);
            foreach (var v in versions)
            {
                if (File.Exists(v.StoragePath)) File.Delete(v.StoragePath);
            }

            // Delete entire file directory
            var workspaceStorage = Path.Combine(Directory.GetCurrentDirectory(), "uploads", file.WorkspaceId.ToString());
            var fileFolder = Path.Combine(workspaceStorage, file.Id.ToString());
            if (Directory.Exists(fileFolder)) Directory.Delete(fileFolder, true);

            await fileRepo.HardDeleteFileAsync(file.Id);
        }
    }

    private async Task CleanupExpiredVersionsAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Safe Cleanup Worker...");

        using var scope = _serviceProvider.CreateScope();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using var connection = connectionFactory.Create();

        // 1. Fetch expired versions that have been expired for at least 1 hour (safety buffer)
        var sqlFetch = @"
            SELECT * FROM versions 
            WHERE expired_at IS NOT NULL 
              AND expired_at + INTERVAL '1 hour' < NOW() 
            LIMIT 100;"; // Batch limit 100

        var expiredVersions = (await connection.QueryAsync<VersionEntity>(sqlFetch)).ToList();

        if (!expiredVersions.Any()) return;

        var db = _redis.GetDatabase();

        foreach (var version in expiredVersions)
        {
            if (stoppingToken.IsCancellationRequested) break;

            // 2. Preload context: Check if it's the latest or nearest full base
            var latestVersion = await connection.QuerySingleOrDefaultAsync<VersionEntity>(
                "SELECT * FROM versions WHERE file_id = @FileId ORDER BY version_number DESC LIMIT 1",
                new { version.FileId });

            if (latestVersion != null && latestVersion.Id == version.Id)
            {
                // Can't delete latest
                continue;
            }

            var nearestFullBase = await connection.QuerySingleOrDefaultAsync<VersionEntity>(
                @"SELECT * FROM versions 
                  WHERE file_id = @FileId AND is_full = true AND version_number <= @CurrentVersionNumber 
                  ORDER BY version_number DESC LIMIT 1",
                new { version.FileId, CurrentVersionNumber = latestVersion?.VersionNumber ?? version.VersionNumber });

            if (nearestFullBase != null && nearestFullBase.Id == version.Id)
            {
                // Can't delete nearest full base
                continue;
            }

            // Check for dependencies (if any version depends on this one as base_version_id)
            var hasDependencies = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM versions WHERE base_version_id = @Id)", new { version.Id });

            if (hasDependencies) continue;

            // 3. Double check active reference (Redis INCR/DECR)
            var activeRefKey = $"file:{version.FileId}:active_ref:{version.Id}";
            var activeRefCountStr = await db.StringGetAsync(activeRefKey);

            if (int.TryParse((string?)activeRefCountStr, out var refCount) && refCount > 0)
            {
                _logger.LogWarning($"Skipping version {version.Id} because it is currently being downloaded.");
                continue; // Is being downloaded
            }

            // 4. Safe to delete
            if (File.Exists(version.StoragePath))
            {
                File.Delete(version.StoragePath);
            }

            // Xóa Redis Cache Reconstruct của file này nếu có
            await db.KeyDeleteAsync($"file:{version.FileId}:reconstruct:{version.Id}");

            // Xóa record trong database
            await connection.ExecuteAsync("DELETE FROM versions WHERE id = @Id", new { version.Id });

            _logger.LogInformation($"Successfully deleted expired version {version.Id}");
        }
    }
}
