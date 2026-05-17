namespace backend.Application.Interfaces
{
    using backend.Domain.Entities;
    public interface IUserRepository
    {
        Task<Guid> Create(string username, string email, string passwordHash);
        Task<User?> GetByEmail(string email);
        Task<User?> GetByIdAsync(Guid id);
        Task<bool> UpdateProfileAsync(Guid id, string? username, string? bio, string? role, string? team, string? division);
        Task<bool> UpdatePasswordAsync(Guid id, string newPasswordHash);
        
        Task CreatePasswordResetTokenAsync(Guid userId, string token, DateTime expiredAt);
        Task<Guid?> GetUserIdByValidResetTokenAsync(string token);
        Task DeletePasswordResetTokenAsync(string token);
    }
}
