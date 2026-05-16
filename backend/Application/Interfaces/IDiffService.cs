using backend.Application.DTOs.File;

namespace backend.Application.Interfaces;

public interface IDiffService
{
    Task<byte[]> ReconstructFileContentAsync(Guid fileId, Guid targetVersionId);
    Task<PreviewResponse> GetPreviewAsync(Guid fileId, Guid versionId);
    Task<DiffResponse> GetDiffAsync(Guid fileId, Guid baseVersionId, Guid targetVersionId);
}
