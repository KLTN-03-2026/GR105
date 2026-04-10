using backend.Domain.Entities;

namespace backend.Application.Interfaces
{
    public interface IWorkspaceRepository
    {
        Task<Workspace> CreateAsync(string name, Guid ownerUserId);
        Task<Workspace?> GetByIdAsync(Guid id);
        Task<IEnumerable<Workspace>> GetOwnedAsync(Guid ownerUserId);
        Task<bool> UpdateAsync(Guid id, string newName);
        Task<bool> DeleteAsync(Guid id);
    }
}
