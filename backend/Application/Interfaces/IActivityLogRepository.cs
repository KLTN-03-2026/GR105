using backend.Application.DTOs.Workspace;

namespace backend.Application.Interfaces
{
    public interface IActivityLogRepository
    {
        Task LogActivityAsync(Guid? userId, Guid? workspaceId, string action, string? entityType, Guid? entityId, string? entityName = null);
        Task<IEnumerable<ActivityLogResponseDto>> GetWorkspaceLogsAsync(Guid workspaceId, int limit = 50, int offset = 0);
    }
}
