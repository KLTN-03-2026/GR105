using backend.Application.DTOs.Admin;

namespace backend.Application.Interfaces
{
    public interface IAdminRepository
    {
        // User Management
        Task<IEnumerable<AdminUserResponse>> GetAllUsersAsync(string? search, int limit, int offset);
        Task<int> GetTotalUsersCountAsync(string? search);
        Task<Guid> CreateUserAsync(string username, string email, string passwordHash, string globalRole);
        Task<bool> UpdateUserAsync(Guid userId, string username, string globalRole);
        Task<bool> ToggleUserLockStatusAsync(Guid userId, bool isLocked);

        // Workspace Management
        Task<IEnumerable<AdminWorkspaceResponse>> GetAllWorkspacesAsync(int limit, int offset);
        Task<int> GetTotalWorkspacesCountAsync();
        Task<AdminWorkspaceResponse?> GetWorkspaceByIdAsync(Guid workspaceId);
        Task<bool> DeleteWorkspaceAsync(Guid workspaceId); // Hard delete

        // System Logs
        Task<IEnumerable<AdminActivityLogResponse>> GetAllActivityLogsAsync(Guid? userId, Guid? workspaceId, string? action, int limit, int offset);
        Task<int> GetTotalActivityLogsCountAsync(Guid? userId, Guid? workspaceId, string? action);

        // Feedbacks
        Task<IEnumerable<AdminFeedbackResponse>> GetAllFeedbacksAsync(string? status, int limit, int offset);
        Task<int> GetTotalFeedbacksCountAsync(string? status);
        Task<bool> UpdateFeedbackStatusAsync(Guid feedbackId, string status);

        // Password Reset Requests
        Task<IEnumerable<AdminPasswordResetRequestResponse>> GetAllPasswordResetRequestsAsync(int limit, int offset);
        Task<int> GetTotalPasswordResetRequestsCountAsync();
    }
}
