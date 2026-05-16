using backend.Application.Common.Exceptions;
using backend.Application.DTOs.Workspace;
using backend.Application.Interfaces;
using backend.Domain.Entities;
using StackExchange.Redis;
using System.Text.Json;

namespace backend.Application.Services
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDatabase _redisDb;

        public WorkspaceService(
            IWorkspaceRepository workspaceRepository,
            IActivityLogRepository activityLogRepository,
            IUserRepository userRepository,
            IConnectionMultiplexer redis)
        {
            _workspaceRepository = workspaceRepository;
            _activityLogRepository = activityLogRepository;
            _userRepository = userRepository;
            _redisDb = redis.GetDatabase();
        }

        private string GetRedisKey(Guid userId) => $"user:{userId}:owned_workspaces";
        private string GetMembersRedisKey(Guid workspaceId) => $"workspace:{workspaceId}:members";

        public async Task<WorkspaceResponse> CreateWorkspaceAsync(CreateWorkspaceRequest request, Guid ownerUserId)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 255)
            {
                throw new ValidationException("Workspace name is invalid (empty or too long).");
            }

            var workspace = await _workspaceRepository.CreateAsync(request.Name.Trim(), ownerUserId);

            await _activityLogRepository.LogActivityAsync(ownerUserId, workspace.Id, "CREATE_WORKSPACE", "workspace", workspace.Id);
            await _redisDb.KeyDeleteAsync(GetRedisKey(ownerUserId));

            return new WorkspaceResponse
            {
                Id = workspace.Id,
                Name = workspace.Name,
                OwnerUserId = workspace.OwnerUserId,
                CreatedAt = workspace.CreatedAt
            };
        }

        public async Task<IEnumerable<WorkspaceResponse>> GetOwnedWorkspacesAsync(Guid ownerUserId)
        {
            var cacheKey = GetRedisKey(ownerUserId);
            var cachedData = await _redisDb.StringGetAsync(cacheKey);

            if (cachedData.HasValue)
            {
                return JsonSerializer.Deserialize<IEnumerable<WorkspaceResponse>>(cachedData.ToString()!) ?? new List<WorkspaceResponse>();
            }

            var workspaces = await _workspaceRepository.GetOwnedAsync(ownerUserId);
            var response = workspaces.Select(w => new WorkspaceResponse
            {
                Id = w.Id,
                Name = w.Name,
                OwnerUserId = w.OwnerUserId,
                CreatedAt = w.CreatedAt
            }).ToList();

            await _redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(response), TimeSpan.FromMinutes(5));

            return response;
        }

        public async Task<bool> UpdateWorkspaceAsync(Guid id, UpdateWorkspaceRequest request, Guid ownerUserId)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 255)
            {
                throw new ValidationException("Workspace name is invalid (empty or too long).");
            }

            var ws = await _workspaceRepository.GetByIdAsync(id);
            if (ws == null) throw new NotFoundException("Workspace not found.");
            if (ws.OwnerUserId != ownerUserId) throw new ForbiddenException("You are not the owner of this workspace.");

            var updated = await _workspaceRepository.UpdateAsync(id, request.Name.Trim());

            if (updated)
            {
                await _activityLogRepository.LogActivityAsync(ownerUserId, id, "UPDATE_WORKSPACE", "workspace", id);
                await _redisDb.KeyDeleteAsync(GetRedisKey(ownerUserId));
            }

            return updated;
        }

        public async Task<bool> DeleteWorkspaceAsync(Guid id, Guid ownerUserId)
        {
            var ws = await _workspaceRepository.GetByIdAsync(id);
            if (ws == null) throw new NotFoundException("Workspace not found.");
            if (ws.OwnerUserId != ownerUserId) throw new ForbiddenException("You are not the owner of this workspace.");

            var deleted = await _workspaceRepository.DeleteAsync(id);

            if (deleted)
            {
                await _activityLogRepository.LogActivityAsync(ownerUserId, null, "DELETE_WORKSPACE", "workspace", id);
                await _redisDb.KeyDeleteAsync(GetRedisKey(ownerUserId));
                await _redisDb.KeyDeleteAsync(GetMembersRedisKey(id));
            }

            return deleted;
        }

        public async Task<bool> AddMemberAsync(Guid workspaceId, AddWorkspaceMemberRequest request, Guid requesterUserId)
        {
            var ws = await _workspaceRepository.GetByIdAsync(workspaceId);
            if (ws == null) throw new NotFoundException("Workspace not found.");
            if (ws.OwnerUserId != requesterUserId) throw new ForbiddenException("Only owner can add members.");

            var userToAdd = await _userRepository.GetByEmail(request.Email);
            if (userToAdd == null) throw new NotFoundException("User to add not found.");

            if (userToAdd.Id == ws.OwnerUserId) throw new ValidationException("Cannot add owner as member again.");

            var role = string.IsNullOrWhiteSpace(request.Role) ? "member" : request.Role.ToLower();
            if (role != "member" && role != "viewer") role = "member";

            var added = await _workspaceRepository.AddMemberAsync(workspaceId, userToAdd.Id, role);
            if (added)
            {
                await _activityLogRepository.LogActivityAsync(requesterUserId, workspaceId, "ADD_MEMBER", "user", userToAdd.Id);
                await _redisDb.KeyDeleteAsync(GetMembersRedisKey(workspaceId));
            }

            return added;
        }

        public async Task<IEnumerable<WorkspaceMemberDto>> GetMembersAsync(Guid workspaceId, Guid requesterUserId)
        {
            var isMember = await _workspaceRepository.IsMemberAsync(workspaceId, requesterUserId);
            if (!isMember) throw new ForbiddenException("You do not have access to this workspace.");

            var cacheKey = GetMembersRedisKey(workspaceId);
            var cachedData = await _redisDb.StringGetAsync(cacheKey);
            if (cachedData.HasValue)
            {
                return JsonSerializer.Deserialize<IEnumerable<WorkspaceMemberDto>>(cachedData.ToString()!) ?? new List<WorkspaceMemberDto>();
            }

            var members = await _workspaceRepository.GetMembersAsync(workspaceId);
            await _redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(members), TimeSpan.FromMinutes(5));
            return members;
        }

        public async Task<bool> RemoveMemberAsync(Guid workspaceId, Guid userIdToRemove, Guid requesterUserId)
        {
            var ws = await _workspaceRepository.GetByIdAsync(workspaceId);
            if (ws == null) throw new NotFoundException("Workspace not found.");
            if (ws.OwnerUserId != requesterUserId) throw new ForbiddenException("Only owner can remove members.");

            if (userIdToRemove == ws.OwnerUserId) throw new ValidationException("Cannot remove the owner of the workspace.");

            var removed = await _workspaceRepository.RemoveMemberAsync(workspaceId, userIdToRemove);
            if (removed)
            {
                await _activityLogRepository.LogActivityAsync(requesterUserId, workspaceId, "REMOVE_MEMBER", "user", userIdToRemove);
                await _redisDb.KeyDeleteAsync(GetMembersRedisKey(workspaceId));
            }

            return removed;
        }

        // ==========================================
        // Quản lý lời mời & Tham gia (UC8)
        // ==========================================

        public async Task<GenerateInviteCodeResponse> GenerateInviteCodeAsync(Guid workspaceId, Guid requesterUserId)
        {
            var ws = await _workspaceRepository.GetByIdAsync(workspaceId);
            if (ws == null) throw new NotFoundException("Workspace not found.");
            if (ws.OwnerUserId != requesterUserId) throw new ForbiddenException("Only owner can generate invite code.");

            var newCode = "w_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            await _workspaceRepository.UpdateInviteSettingsAsync(workspaceId, newCode, ws.InviteEnabled);

            return new GenerateInviteCodeResponse
            {
                InviteCode = newCode,
                InviteEnabled = ws.InviteEnabled
            };
        }

        public async Task<bool> ToggleInviteLinkAsync(Guid workspaceId, bool inviteEnabled, Guid requesterUserId)
        {
            var ws = await _workspaceRepository.GetByIdAsync(workspaceId);
            if (ws == null) throw new NotFoundException("Workspace not found.");
            if (ws.OwnerUserId != requesterUserId) throw new ForbiddenException("Only owner can update invite settings.");

            await _workspaceRepository.UpdateInviteSettingsAsync(workspaceId, ws.InviteCode, inviteEnabled);
            return true;
        }

        public async Task<bool> JoinByCodeAsync(string inviteCode, Guid userId)
        {
            var ws = await _workspaceRepository.GetByInviteCodeAsync(inviteCode);
            if (ws == null || !ws.InviteEnabled) throw new ValidationException("Invalid or disabled invite code.");

            var isAlreadyMember = await _workspaceRepository.IsMemberAsync(ws.Id, userId);
            if (isAlreadyMember) return true; // Idempotent

            var added = await _workspaceRepository.AddMemberAsync(ws.Id, userId, "member");
            if (added)
            {
                await _activityLogRepository.LogActivityAsync(userId, ws.Id, "JOIN_WORKSPACE", "workspace", ws.Id);
                await _redisDb.KeyDeleteAsync(GetMembersRedisKey(ws.Id));
            }
            return added;
        }

        public async Task<string> InviteByEmailAsync(Guid workspaceId, string email, Guid requesterUserId)
        {
            var ws = await _workspaceRepository.GetByIdAsync(workspaceId);
            if (ws == null) throw new NotFoundException("Workspace not found.");
            if (ws.OwnerUserId != requesterUserId) throw new ForbiddenException("Only owner can invite members.");

            var token = Guid.NewGuid().ToString("N");
            var invitation = new WorkspaceInvitation
            {
                WorkspaceId = workspaceId,
                Email = email.Trim().ToLowerInvariant(),
                Token = token,
                ExpiredAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _workspaceRepository.CreateInvitationAsync(invitation);
            
            // Tạm thời trả về token. Ở thực tế sẽ gửi email chứa link có token này.
            return token;
        }

        public async Task<bool> AcceptInviteAsync(string token, Guid userId)
        {
            var invitation = await _workspaceRepository.GetInvitationByTokenAsync(token);
            if (invitation == null) throw new ValidationException("Invalid invite token.");
            if (invitation.UsedAt.HasValue) throw new ValidationException("Invite token already used.");
            if (invitation.ExpiredAt < DateTime.UtcNow) throw new ValidationException("Invite token expired.");

            var currentUser = await _userRepository.GetByIdAsync(userId);
            if (currentUser == null) throw new NotFoundException("User not found.");
            if (currentUser.Email.Trim().ToLowerInvariant() != invitation.Email)
            {
                throw new ForbiddenException("Invite email does not match your registered email.");
            }

            var isAlreadyMember = await _workspaceRepository.IsMemberAsync(invitation.WorkspaceId, userId);
            if (!isAlreadyMember)
            {
                var added = await _workspaceRepository.AddMemberAsync(invitation.WorkspaceId, userId, "member");
                if (added)
                {
                    await _activityLogRepository.LogActivityAsync(userId, invitation.WorkspaceId, "JOIN_WORKSPACE", "workspace", invitation.WorkspaceId);
                    await _redisDb.KeyDeleteAsync(GetMembersRedisKey(invitation.WorkspaceId));
                }
            }

            await _workspaceRepository.MarkInvitationAsUsedAsync(invitation.Id);
            return true;
        }

        public async Task<bool> LeaveWorkspaceAsync(Guid workspaceId, Guid userId)
        {
            var ws = await _workspaceRepository.GetByIdAsync(workspaceId);
            if (ws == null) throw new NotFoundException("Workspace not found.");

            if (ws.OwnerUserId == userId)
            {
                throw new ValidationException("Owner cannot leave the workspace. Transfer ownership or delete it instead.");
            }

            var isMember = await _workspaceRepository.IsMemberAsync(workspaceId, userId);
            if (!isMember) throw new NotFoundException("You are not a member of this workspace.");

            var removed = await _workspaceRepository.RemoveMemberAsync(workspaceId, userId);
            if (removed)
            {
                await _activityLogRepository.LogActivityAsync(userId, workspaceId, "LEAVE_WORKSPACE", "workspace", workspaceId);
                await _redisDb.KeyDeleteAsync(GetMembersRedisKey(workspaceId));
            }
            return removed;
        }
    }
}