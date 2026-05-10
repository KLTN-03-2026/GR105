using backend.Application.DTOs.File;

namespace backend.Application.Interfaces;

public interface IMediaService
{
    Task<MediaResult> GetMediaAsync(Guid userId, Guid fileId, Guid versionId);
}
