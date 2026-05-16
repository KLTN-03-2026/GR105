using backend.Application.DTOs.File;
using backend.Application.Interfaces;
using backend.Domain.Entities;
using StackExchange.Redis;
using Microsoft.AspNetCore.Http;
using backend.Application.Common.Exceptions;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace backend.Application.Services;

public class FileService : IFileService
{
    private readonly IFileRepository _fileRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDbSession _dbSession;
    private readonly IDiffService _diffService;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly string _storagePath;

    public FileService(
        IFileRepository fileRepository,
        IWorkspaceRepository workspaceRepository,
        IConnectionMultiplexer redis,
        IDbSession dbSession,
        IDiffService diffService,
        IBackgroundTaskQueue taskQueue,
        IServiceScopeFactory serviceScopeFactory,
        IActivityLogRepository activityLogRepository)
    {
        _fileRepository = fileRepository;
        _workspaceRepository = workspaceRepository;
        _redis = redis;
        _dbSession = dbSession;
        _diffService = diffService;
        _taskQueue = taskQueue;
        _serviceScopeFactory = serviceScopeFactory;
        _activityLogRepository = activityLogRepository;
        _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

        if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
    }

    private async Task EnsureUserAccessAsync(Guid userId, Guid workspaceId)
    {
        var isInWorkspace = await _workspaceRepository.IsUserInWorkspaceAsync(userId, workspaceId);
        if (!isInWorkspace) throw new ForbiddenException("You do not have access to this workspace.");
    }

    public async Task<FileResponse> UploadFileAsync(Guid workspaceId, Guid userId, UploadFileRequest request)
    {
        await EnsureUserAccessAsync(userId, workspaceId);
        if (request.File == null || request.File.Length == 0) throw new ValidationException("File is empty.");

        ValidateFile(request.File); // Expert Validation

        var ext = Path.GetExtension(request.File.FileName);
        var sanitizedTitle = request.Title ?? request.File.FileName;
        if (!string.IsNullOrEmpty(ext) && !sanitizedTitle.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
        {
            sanitizedTitle += ext;
        }
        var sanitizedFolderPath = request.FolderPath ?? "/";

        var db = _redis.GetDatabase();
        var lockKey = $"lock:upload:{workspaceId}:{sanitizedTitle}";
        var lockToken = Guid.NewGuid().ToString();

        if (!await db.LockTakeAsync(lockKey, lockToken, TimeSpan.FromSeconds(15)))
            throw new ConflictException("Another upload is in progress.");

        try
        {
            FileEntity? file = null;
            var isNewFile = false;

            if (request.BaseVersionId == null)
            {
                file = new FileEntity { Id = Guid.NewGuid(), WorkspaceId = workspaceId, Title = sanitizedTitle, FolderPath = sanitizedFolderPath, CreatedBy = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                isNewFile = true;
            }
            else
            {
                var baseVersion = await _fileRepository.GetVersionByIdAsync(request.BaseVersionId.Value);
                if (baseVersion == null) throw new NotFoundException("Base version not found.");
                file = await _fileRepository.GetFileByIdAsync(baseVersion.FileId);
                if (file == null) throw new NotFoundException("File not found.");

                var currentLatest = await _fileRepository.GetLatestVersionAsync(file.Id);
                if (currentLatest != null && currentLatest.Id != request.BaseVersionId.Value)
                    throw new ConflictException("Conflict: Base version is not the latest.");
            }

            var latestVersion = await _fileRepository.GetLatestVersionAsync(file.Id);
            var newVersionNumber = latestVersion != null ? latestVersion.VersionNumber + 1 : 1;

            // Strategy: Every 5th version is FULL
            var shouldBeFull = (newVersionNumber % 5 == 0) || isNewFile;

            var version = new VersionEntity
            {
                Id = Guid.NewGuid(),
                FileId = file.Id,
                VersionNumber = newVersionNumber,
                BaseVersionId = request.BaseVersionId,
                FileSize = request.File.Length,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                IsFull = shouldBeFull
            };

            var fileFolder = Path.Combine(_storagePath, workspaceId.ToString(), file.Id.ToString());
            if (!Directory.Exists(fileFolder)) Directory.CreateDirectory(fileFolder);
            version.StoragePath = Path.Combine(fileFolder, version.Id.ToString());

            // Process Data
            using var ms = new MemoryStream();
            await request.File.CopyToAsync(ms);
            var incomingBytes = ms.ToArray();

            _dbSession.BeginTransaction();
            try
            {
                if (isNewFile) await _fileRepository.CreateFileAsync(file);

                var fileName = request.File.FileName ?? "";
                var contentType = request.File.ContentType?.ToLowerInvariant() ?? "";
                var isText = contentType.StartsWith("text/") || fileName.EndsWith(".txt") || fileName.EndsWith(".md");

                if (!shouldBeFull && isText && request.BaseVersionId != null)
                {
                    try
                    {
                        var baseBytes = await _diffService.ReconstructFileContentAsync(file.Id, request.BaseVersionId.Value);
                        var baseText = Encoding.UTF8.GetString(baseBytes);
                        var incomingText = Encoding.UTF8.GetString(incomingBytes);

                        var diff = InlineDiffBuilder.Diff(baseText, incomingText);
                        var diffBuilder = new StringBuilder();
                        foreach (var line in diff.Lines)
                        {
                            if (line.Type == ChangeType.Inserted) diffBuilder.AppendLine($"+{line.Text}");
                            else if (line.Type == ChangeType.Deleted) diffBuilder.AppendLine($"-{line.Text}");
                            else diffBuilder.AppendLine($" {line.Text}");
                        }
                        await File.WriteAllTextAsync(version.StoragePath, diffBuilder.ToString());
                        version.IsFull = false;
                    }
                    catch
                    {
                        await File.WriteAllBytesAsync(version.StoragePath, incomingBytes);
                        version.IsFull = true;
                    }
                }
                else
                {
                    await File.WriteAllBytesAsync(version.StoragePath, incomingBytes);
                    version.IsFull = true;
                }

                await _fileRepository.CreateVersionAsync(version);
                await _fileRepository.MarkExpiredVersionsAsync(file.Id, 10);

                _dbSession.Commit();

                // Background tasks: Preload reconstruct & Extract text for search & Invalidate cache
                var fileIdToPreload = file.Id;
                var versionIdToPreload = version.Id;
                var isFullPreload = version.IsFull;
                var workspaceIdToInvalidate = file.WorkspaceId;
                var versionStoragePath = version.StoragePath;
                var versionFileName = file.Title;
                var versionContentType = request.File.ContentType?.ToLowerInvariant() ?? "";

                _ = _taskQueue.QueueBackgroundWorkItemAsync(async token => 
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var diffService = scope.ServiceProvider.GetRequiredService<IDiffService>();
                    var textExtractor = scope.ServiceProvider.GetRequiredService<ITextExtractionService>();
                    var fileRepo = scope.ServiceProvider.GetRequiredService<IFileRepository>();
                    var searchService = scope.ServiceProvider.GetRequiredService<IFileSearchService>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<FileService>>();

                    try
                    {
                        if (!isFullPreload)
                        {
                            await diffService.ReconstructFileContentAsync(fileIdToPreload, versionIdToPreload);
                        }

                        // Extract text
                        var rawText = await textExtractor.ExtractTextAsync(versionStoragePath, versionContentType, versionFileName);
                        if (!string.IsNullOrEmpty(rawText))
                        {
                            await fileRepo.UpdateFileSearchVectorAsync(fileIdToPreload, rawText);
                        }

                        // Invalidate search cache
                        await searchService.InvalidateWorkspaceSearchCacheAsync(workspaceIdToInvalidate);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing background jobs for file {FileId} version {VersionId}", fileIdToPreload, versionIdToPreload);
                    }
                });

                return new FileResponse
                {
                    Id = file.Id,
                    WorkspaceId = file.WorkspaceId,
                    Title = file.Title,
                    FolderPath = file.FolderPath,
                    CreatedBy = file.CreatedBy,
                    CreatedAt = file.CreatedAt,
                    UpdatedAt = file.UpdatedAt,
                    LatestVersionNumber = version.VersionNumber
                };
            }
            catch
            {
                _dbSession.Rollback();
                if (File.Exists(version.StoragePath)) File.Delete(version.StoragePath);
                throw;
            }
        }
        finally
        {
            await db.LockReleaseAsync(lockKey, lockToken);
        }
    }

    public async Task<ReconstructResponse> DownloadFileAsync(Guid userId, Guid fileId, Guid? versionId = null)
    {
        var file = await _fileRepository.GetFileByIdAsync(fileId);
        if (file == null) throw new NotFoundException("File not found.");
        await EnsureUserAccessAsync(userId, file.WorkspaceId);

        var targetVersion = versionId.HasValue
            ? await _fileRepository.GetVersionByIdAsync(versionId.Value)
            : await _fileRepository.GetLatestVersionAsync(fileId);
        if (targetVersion == null) throw new NotFoundException("Version not found.");

        var db = _redis.GetDatabase();
        var activeRefKey = $"file:{fileId}:active_ref:{targetVersion.Id}";
        await db.StringIncrementAsync(activeRefKey);

        try
        {
            var contentBytes = await _diffService.ReconstructFileContentAsync(fileId, targetVersion.Id);
            var ext = Path.GetExtension(file.Title).ToLowerInvariant();
            var contentType = GetContentType(ext);
            return new ReconstructResponse { Stream = new MemoryStream(contentBytes), FileName = file.Title, ContentType = contentType };
        }
        finally
        {
            await db.StringDecrementAsync(activeRefKey);
        }
    }

    private string GetContentType(string ext)
    {
        if (string.IsNullOrEmpty(ext)) return "application/octet-stream";
        
        return ext.ToLowerInvariant().Trim() switch
        {
            ".mp4" => "video/mp4",
            ".mkv" => "video/x-matroska",
            ".webm" => "video/webm",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }

    public async Task<IEnumerable<FileResponse>> GetFilesByWorkspaceAsync(Guid userId, Guid workspaceId, int limit = 100, int offset = 0)
    {
        await EnsureUserAccessAsync(userId, workspaceId);
        var files = await _fileRepository.GetFilesByWorkspaceIdAsync(workspaceId, limit, offset);
        var responses = new List<FileResponse>();
        foreach (var file in files)
        {
            var latest = await _fileRepository.GetLatestVersionAsync(file.Id);
            responses.Add(new FileResponse
            {
                Id = file.Id,
                WorkspaceId = file.WorkspaceId,
                Title = file.Title,
                FolderPath = file.FolderPath,
                CreatedBy = file.CreatedBy,
                CreatedAt = file.CreatedAt,
                UpdatedAt = file.UpdatedAt,
                LatestVersionNumber = latest?.VersionNumber ?? 0
            });
        }
        return responses;
    }

    public async Task<IEnumerable<FileResponse>> GetTrashFilesAsync(Guid userId, Guid workspaceId, int limit = 100, int offset = 0)
    {
        await EnsureUserAccessAsync(userId, workspaceId);
        var files = await _fileRepository.GetTrashFilesAsync(workspaceId, limit, offset);
        return files.Select(f => new FileResponse
        {
            Id = f.Id,
            WorkspaceId = f.WorkspaceId,
            Title = f.Title,
            FolderPath = f.FolderPath,
            CreatedBy = f.CreatedBy,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt,
            DeletedAt = f.DeletedAt
        });
    }

    public async Task<bool> SoftDeleteFileAsync(Guid userId, Guid fileId)
    {
        var file = await _fileRepository.GetFileByIdAsync(fileId);
        if (file == null) throw new NotFoundException("File not found.");
        await EnsureUserAccessAsync(userId, file.WorkspaceId);
        
        var success = await _fileRepository.SoftDeleteFileAsync(fileId);
        if (success)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var searchService = scope.ServiceProvider.GetRequiredService<IFileSearchService>();
            await searchService.InvalidateWorkspaceSearchCacheAsync(file.WorkspaceId);
            
            await _activityLogRepository.LogActivityAsync(userId, file.WorkspaceId, "DELETE_FILE", "file", fileId, file.Title);
        }
        return success;
    }

    public async Task<bool> RestoreFileAsync(Guid userId, Guid fileId)
    {
        var success = await _fileRepository.RestoreFileAsync(fileId);
        if (!success) throw new NotFoundException("File not found in trash.");

        // Need file to get workspaceId for cache invalidation
        var file = await _fileRepository.GetFileByIdAsync(fileId);
        if (file != null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var searchService = scope.ServiceProvider.GetRequiredService<IFileSearchService>();
            await searchService.InvalidateWorkspaceSearchCacheAsync(file.WorkspaceId);
            
            await _activityLogRepository.LogActivityAsync(userId, file.WorkspaceId, "RESTORE_VERSION", "file", fileId, file.Title);
        }

        return true;
    }

    public async Task<IEnumerable<VersionResponse>> GetFileVersionsAsync(Guid userId, Guid fileId)
    {
        var file = await _fileRepository.GetFileByIdAsync(fileId);
        if (file == null) throw new NotFoundException("File not found.");
        await EnsureUserAccessAsync(userId, file.WorkspaceId);

        var versions = await _fileRepository.GetVersionsByFileIdAsync(fileId);
        return versions.Select(v => new VersionResponse
        {
            Id = v.Id,
            FileId = v.FileId,
            VersionNumber = v.VersionNumber,
            IsFull = v.IsFull,
            FileSize = v.FileSize,
            Checksum = v.Checksum,
            CreatedBy = v.CreatedBy,
            CreatedAt = v.CreatedAt,
            ExpiredAt = v.ExpiredAt
        });
    }


    public async Task<PreviewResponse> GetPreviewAsync(Guid userId, Guid fileId, Guid versionId)
    {
        var file = await _fileRepository.GetFileByIdAsync(fileId);
        if (file == null) throw new NotFoundException("File not found.");
        await EnsureUserAccessAsync(userId, file.WorkspaceId);
        return await _diffService.GetPreviewAsync(fileId, versionId);
    }

    public async Task<DiffResponse> GetDiffAsync(Guid userId, Guid fileId, Guid baseVersionId, Guid targetVersionId)
    {
        var file = await _fileRepository.GetFileByIdAsync(fileId);
        if (file == null) throw new NotFoundException("File not found.");
        await EnsureUserAccessAsync(userId, file.WorkspaceId);
        return await _diffService.GetDiffAsync(fileId, baseVersionId, targetVersionId);
    }

    private void ValidateFile(IFormFile file)
    {
        var fileName = file.FileName ?? "";
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = file.ContentType?.ToLowerInvariant() ?? "";
        
        var isText = contentType.StartsWith("text/") || ext == ".txt" || ext == ".md";
        var isMedia = contentType.StartsWith("image/") || contentType.StartsWith("video/");

        long defaultMaxMB = long.TryParse(Environment.GetEnvironmentVariable("MAX_UPLOAD_SIZE_MB"), out var max) ? max : 500;
        long textMaxMB = long.TryParse(Environment.GetEnvironmentVariable("MAX_UPLOAD_SIZE_MB_TEXT"), out var txtMax) ? txtMax : 50;
        long mediaMaxMB = long.TryParse(Environment.GetEnvironmentVariable("MAX_UPLOAD_SIZE_MB_MEDIA"), out var medMax) ? medMax : 500;

        long maxSize = defaultMaxMB * 1024 * 1024;
        if (isText) maxSize = textMaxMB * 1024 * 1024;
        else if (isMedia) maxSize = mediaMaxMB * 1024 * 1024;

        if (file.Length > maxSize) throw new ValidationException($"File too large. Max: {maxSize / 1024 / 1024}MB");
    }
}
