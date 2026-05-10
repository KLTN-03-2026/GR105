using backend.Application.DTOs.File;
using backend.Application.Interfaces;
using backend.Application.Common.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using System.IO;
using StackExchange.Redis;

namespace backend.Application.Services;

public class MediaService : IMediaService
{
    private readonly IFileRepository _fileRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IDiffService _diffService;
    private readonly IConnectionMultiplexer _redis;
    private readonly string _previewStoragePath;

    public MediaService(IFileRepository fileRepository, IWorkspaceRepository workspaceRepository, IDiffService diffService, IConnectionMultiplexer redis)
    {
        _fileRepository = fileRepository;
        _workspaceRepository = workspaceRepository;
        _diffService = diffService;
        _redis = redis;
        _previewStoragePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "previews");

        if (!Directory.Exists(_previewStoragePath)) Directory.CreateDirectory(_previewStoragePath);
    }

    private async Task EnsureUserAccessAsync(Guid userId, Guid workspaceId)
    {
        var isInWorkspace = await _workspaceRepository.IsUserInWorkspaceAsync(userId, workspaceId);
        if (!isInWorkspace) throw new ForbiddenException("You do not have access to this workspace.");
    }

    public async Task<MediaResult> GetMediaAsync(Guid userId, Guid fileId, Guid versionId)
    {
        var file = await _fileRepository.GetFileByIdAsync(fileId);
        if (file == null) throw new NotFoundException("File not found.");
        
        await EnsureUserAccessAsync(userId, file.WorkspaceId);

        var version = await _fileRepository.GetVersionByIdAsync(versionId);
        if (version == null) throw new NotFoundException("Version not found.");

        var ext = Path.GetExtension(file.Title).ToLowerInvariant();
        var contentType = GetContentType(ext);

        if (contentType.StartsWith("video/") || contentType.StartsWith("audio/"))
        {
            // Streaming uses FileStream directly to allow 206 Partial Content
            var stream = new FileStream(version.StoragePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return new MediaResult { Type = MediaType.Stream, Stream = stream, ContentType = contentType, FileExtension = ext };
        }
        else if (contentType.StartsWith("image/"))
        {
            if (version.FileSize <= 1 * 1024 * 1024) // <= 1MB threshold
            {
                var stream = new FileStream(version.StoragePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return new MediaResult { Type = MediaType.Stream, Stream = stream, ContentType = contentType, FileExtension = ext };
            }
            else
            {
                var webpPath = Path.Combine(_previewStoragePath, $"{versionId}.webp");
                
                // Distributed lock for concurrent conversions
                if (!File.Exists(webpPath))
                {
                    var db = _redis.GetDatabase();
                    var lockKey = $"lock:webp:{versionId}";
                    var lockValue = Guid.NewGuid().ToString();

                    var acquired = await db.LockTakeAsync(lockKey, lockValue, TimeSpan.FromSeconds(30));
                    
                    if (!acquired)
                    {
                        // Wait briefly, assume other process is generating it
                        int retries = 0;
                        while (!File.Exists(webpPath) && retries < 10)
                        {
                            await Task.Delay(200);
                            retries++;
                        }
                    }
                    else
                    {
                        try
                        {
                            if (!File.Exists(webpPath))
                            {
                                byte[] originalBytes;
                                if (version.IsFull)
                                {
                                    originalBytes = await File.ReadAllBytesAsync(version.StoragePath);
                                }
                                else 
                                {
                                    originalBytes = await _diffService.ReconstructFileContentAsync(fileId, versionId);
                                }

                                // Convert to WebP using ImageSharp
                                using var image = Image.Load(originalBytes);
                                await image.SaveAsWebpAsync(webpPath, new WebpEncoder { Quality = 75 });
                            }
                        }
                        finally
                        {
                            await db.LockReleaseAsync(lockKey, lockValue);
                        }
                    }
                }

                // If file still doesn't exist (e.g. lock timeout/fail), fallback to original stream
                if (!File.Exists(webpPath))
                {
                    var origStream = new FileStream(version.StoragePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    return new MediaResult { Type = MediaType.Stream, Stream = origStream, ContentType = contentType, FileExtension = ext };
                }

                var stream = new FileStream(webpPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return new MediaResult { Type = MediaType.Stream, Stream = stream, ContentType = "image/webp", FileExtension = ".webp" };
            }
        }
        else if (contentType.StartsWith("text/") || ext == ".md" || ext == ".txt" || ext == ".docx")
        {
            var preview = await _diffService.GetPreviewAsync(fileId, versionId);
            return new MediaResult { Type = MediaType.Text, TextContent = preview.Content, ContentType = "text/plain", FileExtension = ext };
        }
        
        return new MediaResult { Type = MediaType.Error, Error = "Viewing this file type is not supported. Please download.", FileExtension = ext };
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
}
