using backend.Application.DTOs.Admin;
using backend.Application.Interfaces;
using backend.Domain.Entities;

namespace backend.Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IUserRepository _userRepository; // to check existing email
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly IUserContext _userContext;

        public AdminService(
            IAdminRepository adminRepository,
            IUserRepository userRepository,
            IActivityLogRepository activityLogRepository,
            IUserContext userContext)
        {
            _adminRepository = adminRepository;
            _userRepository = userRepository;
            _activityLogRepository = activityLogRepository;
            _userContext = userContext;
        }

        public async Task<(IEnumerable<AdminUserResponse> Items, int TotalCount)> GetAllUsersAsync(string? search, int page, int pageSize)
        {
            int limit = pageSize > 0 ? pageSize : 20;
            int offset = (page > 0 ? page - 1 : 0) * limit;

            var items = await _adminRepository.GetAllUsersAsync(search, limit, offset);
            var totalCount = await _adminRepository.GetTotalUsersCountAsync(search);

            return (items, totalCount);
        }

        public async Task<Guid> CreateUserAsync(CreateAdminUserRequest request)
        {
            var existingUser = await _userRepository.GetByEmail(request.Email);
            if (existingUser != null)
            {
                throw new Exception("Email is already in use.");
            }

            // Hash password just like regular registration
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            return await _adminRepository.CreateUserAsync(request.Username, request.Email, passwordHash, request.Role);
        }

        public async Task<bool> UpdateUserAsync(Guid userId, UpdateAdminUserRequest request)
        {
            return await _adminRepository.UpdateUserAsync(userId, request.Username, request.Role);
        }

        public async Task<bool> ToggleUserLockStatusAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            return await _adminRepository.ToggleUserLockStatusAsync(userId, !user.IsLocked);
        }

        public async Task<(IEnumerable<AdminWorkspaceResponse> Items, int TotalCount)> GetAllWorkspacesAsync(int page, int pageSize)
        {
            int limit = pageSize > 0 ? pageSize : 20;
            int offset = (page > 0 ? page - 1 : 0) * limit;

            var items = await _adminRepository.GetAllWorkspacesAsync(limit, offset);
            var totalCount = await _adminRepository.GetTotalWorkspacesCountAsync();

            return (items, totalCount);
        }

        public async Task<AdminWorkspaceResponse?> GetWorkspaceByIdAsync(Guid workspaceId)
        {
            return await _adminRepository.GetWorkspaceByIdAsync(workspaceId);
        }

        public async Task<bool> DeleteWorkspaceAsync(Guid workspaceId)
        {
            var success = await _adminRepository.DeleteWorkspaceAsync(workspaceId);
            if (success)
            {
                var adminUserId = _userContext.UserId;
                await _activityLogRepository.LogActivityAsync(
                    adminUserId,
                    workspaceId,
                    "DELETE_WORKSPACE",
                    "workspace",
                    workspaceId,
                    "Admin Hard Delete"
                );
            }
            return success;
        }

        public async Task<(IEnumerable<AdminActivityLogResponse> Items, int TotalCount)> GetAllActivityLogsAsync(Guid? userId, Guid? workspaceId, string? action, int page, int pageSize)
        {
            int limit = pageSize > 0 ? pageSize : 20;
            int offset = (page > 0 ? page - 1 : 0) * limit;

            var items = await _adminRepository.GetAllActivityLogsAsync(userId, workspaceId, action, limit, offset);
            var totalCount = await _adminRepository.GetTotalActivityLogsCountAsync(userId, workspaceId, action);

            return (items, totalCount);
        }

        public async Task<(IEnumerable<AdminFeedbackResponse> Items, int TotalCount)> GetAllFeedbacksAsync(string? status, int page, int pageSize)
        {
            int limit = pageSize > 0 ? pageSize : 20;
            int offset = (page > 0 ? page - 1 : 0) * limit;

            var items = await _adminRepository.GetAllFeedbacksAsync(status, limit, offset);
            var totalCount = await _adminRepository.GetTotalFeedbacksCountAsync(status);

            return (items, totalCount);
        }

        public async Task<bool> UpdateFeedbackStatusAsync(Guid feedbackId, string status)
        {
            return await _adminRepository.UpdateFeedbackStatusAsync(feedbackId, status);
        }

        public async Task<(IEnumerable<AdminPasswordResetRequestResponse> Items, int TotalCount)> GetAllPasswordResetRequestsAsync(int page, int pageSize)
        {
            int limit = pageSize > 0 ? pageSize : 20;
            int offset = (page > 0 ? page - 1 : 0) * limit;

            var items = await _adminRepository.GetAllPasswordResetRequestsAsync(limit, offset);
            var totalCount = await _adminRepository.GetTotalPasswordResetRequestsCountAsync();

            return (items, totalCount);
        }
    }
}
