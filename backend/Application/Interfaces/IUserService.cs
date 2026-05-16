using backend.Domain.Entities;
using backend.Application.DTOs.User;

namespace backend.Application.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    }
}
