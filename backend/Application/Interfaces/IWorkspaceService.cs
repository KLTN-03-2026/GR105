using backend.Application.DTOs.Workspace;

namespace backend.Application.Interfaces
{
    public interface IWorkspaceService
    {
        Task<WorkspaceResponse> CreateWorkspaceAsync(CreateWorkspaceRequest request, Guid ownerUserId);
        Task<IEnumerable<WorkspaceResponse>> GetOwnedWorkspacesAsync(Guid ownerUserId);
        Task<bool> UpdateWorkspaceAsync(Guid id, UpdateWorkspaceRequest request, Guid ownerUserId);
        Task<bool> DeleteWorkspaceAsync(Guid id, Guid ownerUserId);
    }
}
