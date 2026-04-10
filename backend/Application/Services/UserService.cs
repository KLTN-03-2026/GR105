namespace backend.Application.Services;

using backend.Application.Interfaces;
using backend.Domain.Entities;
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        // Đã thay thế mock bằng gọi repository thật
        return await _userRepository.GetByIdAsync(id);
    }
}

