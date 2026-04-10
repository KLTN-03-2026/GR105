using backend.Domain.Entities;

namespace backend.Application.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(Guid id);

    }
}
