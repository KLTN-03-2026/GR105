using backend.Domain.Entities;
using backend.Application.DTOs.File;

namespace backend.Application.Interfaces
{
    public interface IFileRepository
    {
        // File operations
        Task<Guid> CreateFileAsync(FileEntity file);
        Task<FileEntity?> GetFileByIdAsync(Guid fileId);
        Task<IEnumerable<FileEntity>> GetFilesByWorkspaceIdAsync(Guid workspaceId, int limit = 100, int offset = 0);
        Task<IEnumerable<FileEntity>> GetTrashFilesAsync(Guid workspaceId, int limit = 100, int offset = 0);
        Task<bool> SoftDeleteFileAsync(Guid fileId);
        Task<bool> RestoreFileAsync(Guid fileId);
        Task<bool> HardDeleteFileAsync(Guid fileId);

        // File Search
        Task<IEnumerable<FileSearchResultDto>> SearchFilesAsync(Guid workspaceId, string keyword);
        Task UpdateFileSearchVectorAsync(Guid fileId, string rawText);

        // Version operations
        Task<Guid> CreateVersionAsync(VersionEntity version);
        Task<VersionEntity?> GetVersionByIdAsync(Guid versionId);
        Task<IEnumerable<VersionEntity>> GetVersionsByFileIdAsync(Guid fileId, int limit = 100, int offset = 0);

        // Core versioning queries
        Task<VersionEntity?> GetLatestVersionAsync(Guid fileId);
        Task<VersionEntity?> GetNearestFullBaseAsync(Guid fileId, int currentVersionNumber);
        Task<IEnumerable<VersionEntity>> GetVersionsBetweenAsync(Guid fileId, int startVersionNumber, int endVersionNumber);
        Task<int> MarkExpiredVersionsAsync(Guid fileId, int keepCount);
    }
}
