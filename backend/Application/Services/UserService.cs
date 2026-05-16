namespace backend.Application.Services;

using backend.Application.Interfaces;
using backend.Domain.Entities;
using backend.Application.DTOs.User;
using backend.Application.Common.Exceptions;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IActivityLogRepository _activityLogRepository;

    public UserService(IUserRepository userRepository, IActivityLogRepository activityLogRepository)
    {
        _userRepository = userRepository;
        _activityLogRepository = activityLogRepository;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            throw new ValidationException("Username cannot be empty.");

        var updated = await _userRepository.UpdateProfileAsync(userId, request.Username.Trim());
        if (updated)
        {
            await _activityLogRepository.LogActivityAsync(userId, null, "UPDATE_PROFILE", "user", userId);
        }
        return updated;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new NotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
        {
            throw new ValidationException("Incorrect old password.");
        }

        if (request.OldPassword == request.NewPassword)
        {
            throw new ValidationException("New password must be different from the old password.");
        }

        string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        var updated = await _userRepository.UpdatePasswordAsync(userId, newPasswordHash);

        if (updated)
        {
            await _activityLogRepository.LogActivityAsync(userId, null, "CHANGE_PASSWORD", "user", userId);
        }
        return updated;
    }
}

