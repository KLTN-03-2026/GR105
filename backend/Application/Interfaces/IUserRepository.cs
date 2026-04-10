namespace backend.Application.Interfaces
{
    using backend.Domain.Entities;
    public interface IUserRepository
    {
        Task<Guid> Create(string username, string email, string passwordHash);
        Task<User?> GetByEmail(string email);
        Task<User?> GetByIdAsync(Guid id);
    }
}
