using backend.Application.DTOs.User;
using backend.Application.DTOs.Workspace;
using backend.Domain.Entities;

namespace backend.Application.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task<IEnumerable<ActivityLogResponseDto>> GetActivityLogsAsync(Guid userId, int limit = 50, int offset = 0);
    }
}

