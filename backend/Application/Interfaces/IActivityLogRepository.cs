namespace backend.Application.Interfaces
{
    public interface IActivityLogRepository
    {
        Task LogActivityAsync(Guid? userId, Guid? workspaceId, string action, string? entityType, Guid? entityId);
    }
}
