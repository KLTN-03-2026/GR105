using backend.Application.DTOs.Workspace;
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

        // Quản lý thành viên (UC3)
        Task<bool> AddMemberAsync(Guid workspaceId, Guid userId, string role);
        Task<IEnumerable<WorkspaceMemberDto>> GetMembersAsync(Guid workspaceId);
        Task<bool> RemoveMemberAsync(Guid workspaceId, Guid userId);
        Task<bool> IsMemberAsync(Guid workspaceId, Guid userId);
        Task<bool> IsUserInWorkspaceAsync(Guid userId, Guid workspaceId);

        // Quản lý lời mời (UC8)
        Task<Workspace?> GetByInviteCodeAsync(string inviteCode);
        Task<bool> UpdateInviteSettingsAsync(Guid workspaceId, string? inviteCode, bool inviteEnabled);
        Task<WorkspaceInvitation> CreateInvitationAsync(WorkspaceInvitation invitation);
        Task<WorkspaceInvitation?> GetInvitationByTokenAsync(string token);
        Task<bool> MarkInvitationAsUsedAsync(Guid invitationId);
    }
}