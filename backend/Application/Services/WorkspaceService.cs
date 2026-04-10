using backend.Application.Common.Exceptions;
using backend.Application.DTOs.Workspace;
using backend.Application.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace backend.Application.Services
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly IDatabase _redisDb;

        public WorkspaceService(
            IWorkspaceRepository workspaceRepository,
            IActivityLogRepository activityLogRepository,
            IConnectionMultiplexer redis)
        {
            _workspaceRepository = workspaceRepository;
            _activityLogRepository = activityLogRepository;
            _redisDb = redis.GetDatabase();
        }

        private string GetRedisKey(Guid userId) => $"user:{userId}:owned_workspaces";

        public async Task<WorkspaceResponse> CreateWorkspaceAsync(CreateWorkspaceRequest request, Guid ownerUserId)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 255)
            {
                throw new ValidationException("Workspace name is invalid (empty or too long).");
            }

            var workspace = await _workspaceRepository.CreateAsync(request.Name.Trim(), ownerUserId);

            // Log activity: Create Workspace là một hành động (nhưng DB enum không có CREATE_WORKSPACE). 
            // Sẽ dùng CREATE_WORKSPACE sau khi chạy script ALTER TYPE.
            await _activityLogRepository.LogActivityAsync(ownerUserId, workspace.Id, "CREATE_WORKSPACE", "workspace", workspace.Id);

            // Xoá cache để lấy mới
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

            // Cache trong 5 phút
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
                // Note: Bảng ActivityLogs tham chiếu workspace_id (ON DELETE SET NULL),
                // do đó khi log DELETE_WORKSPACE, workspace_id nên để null (do workspace đã xoá)
                await _activityLogRepository.LogActivityAsync(ownerUserId, null, "DELETE_WORKSPACE", "workspace", id);
                await _redisDb.KeyDeleteAsync(GetRedisKey(ownerUserId));
            }

            return deleted;
        }
    }
}
