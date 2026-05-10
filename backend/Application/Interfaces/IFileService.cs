using backend.Application.DTOs.File;

namespace backend.Application.Interfaces
{
    public interface IFileService
    {
        Task<FileResponse> UploadFileAsync(Guid workspaceId, Guid userId, UploadFileRequest request);
        Task<ReconstructResponse> DownloadFileAsync(Guid userId, Guid fileId, Guid? versionId = null);
        Task<IEnumerable<VersionResponse>> GetFileVersionsAsync(Guid userId, Guid fileId);
        Task<IEnumerable<FileResponse>> GetFilesByWorkspaceAsync(Guid userId, Guid workspaceId, int limit = 100, int offset = 0);
        Task<IEnumerable<FileResponse>> GetTrashFilesAsync(Guid userId, Guid workspaceId, int limit = 100, int offset = 0);
        Task<bool> SoftDeleteFileAsync(Guid userId, Guid fileId);
        Task<bool> RestoreFileAsync(Guid userId, Guid fileId);
        Task<PreviewResponse> GetPreviewAsync(Guid userId, Guid fileId, Guid versionId);
        Task<DiffResponse> GetDiffAsync(Guid userId, Guid fileId, Guid baseVersionId, Guid targetVersionId);
    }
}
