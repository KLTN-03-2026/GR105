using backend.Application.DTOs.File;
using backend.Application.Interfaces;
using backend.Domain.Entities;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using StackExchange.Redis;
using System.Text;
using backend.Application.Common.Exceptions;
using DocumentFormat.OpenXml.Packaging;

namespace backend.Application.Services;

public class DiffService : IDiffService
{
    private readonly IFileRepository _fileRepository;
    private readonly IConnectionMultiplexer _redis;

    public DiffService(IFileRepository fileRepository, IConnectionMultiplexer redis)
    {
        _fileRepository = fileRepository;
        _redis = redis;
    }

    public async Task<byte[]> ReconstructFileContentAsync(Guid fileId, Guid targetVersionId)
    {
        var db = _redis.GetDatabase();
        var cacheKey = $"file:{fileId}:reconstruct:{targetVersionId}";
        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue) return (byte[])cached!;

        var targetVersion = await _fileRepository.GetVersionByIdAsync(targetVersionId);
        if (targetVersion == null) throw new NotFoundException("Version not found.");

        var file = await _fileRepository.GetFileByIdAsync(fileId);
        if (file == null) throw new NotFoundException("File not found.");

        var fileCategory = GetFileCategory(file.Title);
        
        // If it's a binary file, diffing was never used, so the nearest base IS the target version, and it's always full.
        // Even if it's docx, it's saved as binary (full). We should just return the raw bytes.
        if (fileCategory == "binary" || fileCategory == "docx")
        {
             var bytes = await File.ReadAllBytesAsync(targetVersion.StoragePath);
             await db.StringSetAsync(cacheKey, bytes, TimeSpan.FromMinutes(10));
             return bytes;
        }

        var nearestBase = await _fileRepository.GetNearestFullBaseAsync(fileId, targetVersion.VersionNumber);
        if (nearestBase == null) throw new Exception("Data corruption: No full base found.");

        var versionsToApply = await _fileRepository.GetVersionsBetweenAsync(fileId, nearestBase.VersionNumber, targetVersion.VersionNumber);

        var currentText = Encoding.UTF8.GetString(await File.ReadAllBytesAsync(nearestBase.StoragePath));

        foreach (var v in versionsToApply.Where(v => v.Id != nearestBase.Id))
        {
            if (v.IsFull)
            {
                currentText = Encoding.UTF8.GetString(await File.ReadAllBytesAsync(v.StoragePath));
            }
            else
            {
                var diffLines = await File.ReadAllLinesAsync(v.StoragePath);
                var baseLines = currentText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                var result = new List<string>();
                int baseIdx = 0;
                foreach (var line in diffLines)
                {
                    if (line.StartsWith("+")) result.Add(line.Substring(1));
                    else if (line.StartsWith("-")) baseIdx++;
                    else if (line.StartsWith(" "))
                    {
                        if (baseIdx < baseLines.Length) result.Add(baseLines[baseIdx++]);
                    }
                }
                currentText = string.Join("\n", result);
            }
        }

        var finalBytes = Encoding.UTF8.GetBytes(currentText);
        await db.StringSetAsync(cacheKey, finalBytes, TimeSpan.FromMinutes(10));
        return finalBytes;
    }

    private string ExtractTextFromDocx(byte[] docxBytes)
    {
        using var ms = new MemoryStream(docxBytes);
        using var wordDocument = WordprocessingDocument.Open(ms, false);
        var body = wordDocument.MainDocumentPart?.Document?.Body;
        return body?.InnerText ?? string.Empty;
    }

    private string GetFileCategory(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext == ".docx") return "docx";
        if (ext == ".txt" || ext == ".md" || ext == ".json" || ext == ".csv" || ext == ".xml") return "text";
        return "binary";
    }

    public async Task<PreviewResponse> GetPreviewAsync(Guid fileId, Guid versionId)
    {
        var file = await _fileRepository.GetFileByIdAsync(fileId);
        if (file == null) throw new NotFoundException("File not found.");

        var fileCategory = GetFileCategory(file.Title);
        if (fileCategory == "binary")
        {
            return new PreviewResponse
            {
                Content = "Preview is not supported for binary files.",
                IsTruncated = false,
                FileType = "binary"
            };
        }

        var contentBytes = await ReconstructFileContentAsync(fileId, versionId);
        string contentString;

        if (fileCategory == "docx")
        {
            try
            {
                contentString = ExtractTextFromDocx(contentBytes);
            }
            catch
            {
                contentString = "Failed to extract text from DOCX.";
            }
        }
        else
        {
            contentString = Encoding.UTF8.GetString(contentBytes);
        }

        // Limit Preview size (Max 2MB).
        const int maxPreviewChars = 2 * 1024 * 1024;
        bool isTruncated = false;

        if (contentString.Length > maxPreviewChars)
        {
            isTruncated = true;
            contentString = contentString.Substring(0, maxPreviewChars);
        }

        return new PreviewResponse
        {
            Content = contentString,
            IsTruncated = isTruncated,
            FileType = fileCategory
        };
    }

    public async Task<DiffResponse> GetDiffAsync(Guid fileId, Guid baseVersionId, Guid targetVersionId)
    {
        var file = await _fileRepository.GetFileByIdAsync(fileId);
        if (file == null) throw new NotFoundException("File not found.");

        var fileCategory = GetFileCategory(file.Title);
        if (fileCategory == "binary")
        {
            return new DiffResponse
            {
                Status = "NotSupported",
                Message = "Diff is not supported for binary files."
            };
        }

        var baseContentBytes = await ReconstructFileContentAsync(fileId, baseVersionId);
        var targetContentBytes = await ReconstructFileContentAsync(fileId, targetVersionId);

        string baseText, targetText;

        if (fileCategory == "docx")
        {
            try
            {
                baseText = ExtractTextFromDocx(baseContentBytes);
                targetText = ExtractTextFromDocx(targetContentBytes);
            }
            catch
            {
                return new DiffResponse
                {
                    Status = "Error",
                    Message = "Failed to extract text from DOCX for diffing."
                };
            }
        }
        else
        {
            baseText = Encoding.UTF8.GetString(baseContentBytes);
            targetText = Encoding.UTF8.GetString(targetContentBytes);
        }

        var diff = InlineDiffBuilder.Diff(baseText, targetText);

        var diffLines = new List<DiffLineDto>();
        int lineOld = 1;
        int lineNew = 1;

        foreach (var line in diff.Lines)
        {
            if (line.Type == ChangeType.Inserted)
            {
                diffLines.Add(new DiffLineDto { Type = "Inserted", Text = line.Text, LineOld = null, LineNew = lineNew++ });
            }
            else if (line.Type == ChangeType.Deleted)
            {
                diffLines.Add(new DiffLineDto { Type = "Deleted", Text = line.Text, LineOld = lineOld++, LineNew = null });
            }
            else if (line.Type == ChangeType.Unchanged)
            {
                diffLines.Add(new DiffLineDto { Type = "Unchanged", Text = line.Text, LineOld = lineOld++, LineNew = lineNew++ });
            }
        }

        return new DiffResponse
        {
            Status = "Success",
            Lines = diffLines
        };
    }
}
