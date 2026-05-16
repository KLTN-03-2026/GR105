using backend.Application.DTOs.Workspace;

namespace backend.Application.Interfaces
{
    public interface IWorkspaceService
    {
        Task<WorkspaceResponse> CreateWorkspaceAsync(CreateWorkspaceRequest request, Guid ownerUserId);
        Task<IEnumerable<WorkspaceResponse>> GetOwnedWorkspacesAsync(Guid ownerUserId);
        Task<bool> UpdateWorkspaceAsync(Guid id, UpdateWorkspaceRequest request, Guid ownerUserId);
        Task<bool> DeleteWorkspaceAsync(Guid id, Guid ownerUserId);

        // Quản lý thành viên (UC3)
        Task<bool> AddMemberAsync(Guid workspaceId, AddWorkspaceMemberRequest request, Guid requesterUserId);
        Task<IEnumerable<WorkspaceMemberDto>> GetMembersAsync(Guid workspaceId, Guid requesterUserId);
        Task<bool> RemoveMemberAsync(Guid workspaceId, Guid userIdToRemove, Guid requesterUserId);

        // Quản lý lời mời & Tham gia (UC8)
        Task<GenerateInviteCodeResponse> GenerateInviteCodeAsync(Guid workspaceId, Guid requesterUserId);
        Task<bool> ToggleInviteLinkAsync(Guid workspaceId, bool inviteEnabled, Guid requesterUserId);
        Task<bool> JoinByCodeAsync(string inviteCode, Guid userId);
        Task<string> InviteByEmailAsync(Guid workspaceId, string email, Guid requesterUserId);
        Task<bool> AcceptInviteAsync(string token, Guid userId);
        Task<bool> LeaveWorkspaceAsync(Guid workspaceId, Guid userId);
    }
}