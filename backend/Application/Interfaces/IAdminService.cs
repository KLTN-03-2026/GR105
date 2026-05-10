using backend.Application.DTOs.Admin;

namespace backend.Application.Interfaces
{
    public interface IAdminService
    {
        Task<(IEnumerable<AdminUserResponse> Items, int TotalCount)> GetAllUsersAsync(string? search, int page, int pageSize);
        Task<Guid> CreateUserAsync(CreateAdminUserRequest request);
        Task<bool> UpdateUserAsync(Guid userId, UpdateAdminUserRequest request);
        Task<bool> ToggleUserLockStatusAsync(Guid userId);

        Task<(IEnumerable<AdminWorkspaceResponse> Items, int TotalCount)> GetAllWorkspacesAsync(int page, int pageSize);
        Task<AdminWorkspaceResponse?> GetWorkspaceByIdAsync(Guid workspaceId);
        Task<bool> DeleteWorkspaceAsync(Guid workspaceId); // Hard delete

        Task<(IEnumerable<AdminActivityLogResponse> Items, int TotalCount)> GetAllActivityLogsAsync(Guid? userId, Guid? workspaceId, string? action, int page, int pageSize);
        
        Task<(IEnumerable<AdminFeedbackResponse> Items, int TotalCount)> GetAllFeedbacksAsync(string? status, int page, int pageSize);
        Task<bool> UpdateFeedbackStatusAsync(Guid feedbackId, string status);

        Task<(IEnumerable<AdminPasswordResetRequestResponse> Items, int TotalCount)> GetAllPasswordResetRequestsAsync(int page, int pageSize);
    }
}
