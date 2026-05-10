using backend.Application.Interfaces;
using backend.Domain.Entities;
using backend.Infrastructure.Persistence;
using backend.Application.DTOs.Workspace;
using Dapper;
using StackExchange.Redis;

namespace backend.Infrastructure.Repositories
{
    public class WorkspaceRepository : IWorkspaceRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IConnectionMultiplexer _redis;

        public WorkspaceRepository(IDbConnectionFactory dbConnectionFactory, IConnectionMultiplexer redis)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _redis = redis;
        }

        private string GetAuthCacheKey(Guid workspaceId, Guid userId) => $"workspace:{workspaceId}:user:{userId}:has_access";

        public async Task<Workspace> CreateAsync(string name, Guid ownerUserId)
        {
            using var connection = _dbConnectionFactory.Create();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                var workspaceSql = @"
                    INSERT INTO workspaces (id, name, owner_user_id, created_at, updated_at)
                    VALUES (@Id, @Name, @OwnerUserId, @CreatedAt, @UpdatedAt)
                    RETURNING id, name, owner_user_id as OwnerUserId, invite_code as InviteCode, invite_enabled as InviteEnabled, created_at as CreatedAt, updated_at as UpdatedAt;";

                var workspaceId = Guid.NewGuid();
                var now = DateTime.UtcNow;

                var workspace = await connection.QuerySingleAsync<Workspace>(workspaceSql, new
                {
                    Id = workspaceId,
                    Name = name,
                    OwnerUserId = ownerUserId,
                    CreatedAt = now,
                    UpdatedAt = now
                }, transaction);

                var workspaceUserSql = @"
                    INSERT INTO workspace_users (user_id, workspace_id, role, joined_at)
                    VALUES (@UserId, @WorkspaceId, @Role, @JoinedAt);";

                await connection.ExecuteAsync(workspaceUserSql, new
                {
                    UserId = ownerUserId,
                    WorkspaceId = workspaceId,
                    Role = "owner",
                    JoinedAt = now
                }, transaction);

                transaction.Commit();
                
                // Cache authorization
                var db = _redis.GetDatabase();
                await db.StringSetAsync(GetAuthCacheKey(workspaceId, ownerUserId), "true", TimeSpan.FromMinutes(10));

                return workspace;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<Workspace?> GetByIdAsync(Guid id)
        {
            using var connection = _dbConnectionFactory.Create();
            var sql = @"
                SELECT id, name, owner_user_id AS OwnerUserId, invite_code as InviteCode, invite_enabled as InviteEnabled, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM workspaces
                WHERE id = @Id";
            return await connection.QuerySingleOrDefaultAsync<Workspace>(sql, new { Id = id });
        }

        public async Task<IEnumerable<Workspace>> GetOwnedAsync(Guid ownerUserId)
        {
            using var connection = _dbConnectionFactory.Create();
            var sql = @"
                SELECT id, name, owner_user_id AS OwnerUserId, invite_code as InviteCode, invite_enabled as InviteEnabled, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM workspaces
                WHERE owner_user_id = @OwnerUserId
                ORDER BY created_at DESC";
            return await connection.QueryAsync<Workspace>(sql, new { OwnerUserId = ownerUserId });
        }

        public async Task<bool> UpdateAsync(Guid id, string newName)
        {
            using var connection = _dbConnectionFactory.Create();
            var sql = @"
                UPDATE workspaces 
                SET name = @Name, updated_at = @UpdatedAt 
                WHERE id = @Id";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                Name = newName,
                UpdatedAt = DateTime.UtcNow,
                Id = id
            });

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            using var connection = _dbConnectionFactory.Create();
            var sql = "DELETE FROM workspaces WHERE id = @Id";
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            
            // Note: Can't easily invalidate all user caches for this workspace without tracking them,
            // but the workspace is deleted so the DB will return false later anyway. Let cache expire.
            return rowsAffected > 0;
        }

        public async Task<bool> AddMemberAsync(Guid workspaceId, Guid userId, string role)
        {
            using var connection = _dbConnectionFactory.Create();
            var sql = @"
                INSERT INTO workspace_users (user_id, workspace_id, role, joined_at)
                VALUES (@UserId, @WorkspaceId, @Role, @JoinedAt)
                ON CONFLICT (user_id, workspace_id) DO NOTHING;";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                WorkspaceId = workspaceId,
                Role = role,
                JoinedAt = DateTime.UtcNow
            });

            if (rowsAffected > 0)
            {
                var db = _redis.GetDatabase();
                await db.StringSetAsync(GetAuthCacheKey(workspaceId, userId), "true", TimeSpan.FromMinutes(10));
            }

            return rowsAffected > 0;
        }

        public async Task<IEnumerable<WorkspaceMemberDto>> GetMembersAsync(Guid workspaceId)
        {
            using var connection = _dbConnectionFactory.Create();
            var sql = @"
                SELECT u.id AS UserId, u.email AS Email, wu.role AS Role, wu.joined_at AS JoinedAt
                FROM workspace_users wu
                JOIN users u ON wu.user_id = u.id
                WHERE wu.workspace_id = @WorkspaceId
                ORDER BY wu.joined_at ASC;";

            return await connection.QueryAsync<WorkspaceMemberDto>(sql, new { WorkspaceId = workspaceId });
        }

        public async Task<bool> RemoveMemberAsync(Guid workspaceId, Guid userId)
        {
            using var connection = _dbConnectionFactory.Create();
            var sql = "DELETE FROM workspace_users WHERE workspace_id = @WorkspaceId AND user_id = @UserId;";
            var rowsAffected = await connection.ExecuteAsync(sql, new { WorkspaceId = workspaceId, UserId = userId });
            
            if (rowsAffected > 0)
            {
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(GetAuthCacheKey(workspaceId, userId));
            }
            
            return rowsAffected > 0;
        }

        public async Task<bool> IsMemberAsync(Guid workspaceId, Guid userId)
        {
            var db = _redis.GetDatabase();
            var cacheKey = GetAuthCacheKey(workspaceId, userId);
            var cached = await db.StringGetAsync(cacheKey);
            if (cached.HasValue) return cached == "true";

            using var connection = _dbConnectionFactory.Create();
            var sql = "SELECT 1 FROM workspace_users WHERE workspace_id = @WorkspaceId AND user_id = @UserId LIMIT 1;";
            var exists = await connection.ExecuteScalarAsync<int?>(sql, new { WorkspaceId = workspaceId, UserId = userId });
            
            var hasAccess = exists != null;
            await db.StringSetAsync(cacheKey, hasAccess ? "true" : "false", TimeSpan.FromMinutes(10));
            
            return hasAccess;
        }

        public async Task<bool> IsUserInWorkspaceAsync(Guid userId, Guid workspaceId)
        {
            return await IsMemberAsync(workspaceId, userId);
        }

        public async Task<Workspace?> GetByInviteCodeAsync(string inviteCode)
        {
            using var connection = _dbConnectionFactory.Create();
            var sql = @"
                SELECT id, name, owner_user_id AS OwnerUserId, invite_code AS InviteCode, invite_enabled AS InviteEnabled, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM workspaces
                WHERE invite_code = @InviteCode";
            return await connection.QuerySingleOrDefaultAsync<Workspace>(sql, new { InviteCode = inviteCode });
        }

        public async Task<bool> UpdateInviteSettingsAsync(Guid workspaceId, string? inviteCode, bool inviteEnabled)
        {
            using var connection = _dbConnectionFactory.Create();
            var sql = @"
                UPDATE workspaces 
                SET invite_code = @InviteCode, invite_enabled = @InviteEnabled, updated_at = @UpdatedAt 
                WHERE id = @Id";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                InviteCode = inviteCode,
                InviteEnabled = inviteEnabled,
                UpdatedAt = DateTime.UtcNow,
                Id = workspaceId
            });

            return rowsAffected > 0;
        }

        public async Task<WorkspaceInvitation> CreateInvitationAsync(WorkspaceInvitation invitation)
        {
            using var connection = _dbConnectionFactory.Create();
            var sql = @"
                INSERT INTO workspace_invitations (workspace_id, email, token, expired_at, used_at, created_at)
                VALUES (@WorkspaceId, @Email, @Token, @ExpiredAt, @UsedAt, @CreatedAt)
                RETURNING id, workspace_id as WorkspaceId, email, token, expired_at as ExpiredAt, used_at as UsedAt, created_at as CreatedAt;";

            return await connection.QuerySingleAsync<WorkspaceInvitation>(sql, invitation);
        }

        public async Task<WorkspaceInvitation?> GetInvitationByTokenAsync(string token)
        {
            using var connection = _dbConnectionFactory.Create();
            var sql = @"
                SELECT id, workspace_id as WorkspaceId, email, token, expired_at as ExpiredAt, used_at as UsedAt, created_at as CreatedAt
                FROM workspace_invitations
                WHERE token = @Token";
            return await connection.QuerySingleOrDefaultAsync<WorkspaceInvitation>(sql, new { Token = token });
        }

        public async Task<bool> MarkInvitationAsUsedAsync(Guid invitationId)
        {
            using var connection = _dbConnectionFactory.Create();
            var sql = @"
                UPDATE workspace_invitations 
                SET used_at = @UsedAt 
                WHERE id = @Id";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                UsedAt = DateTime.UtcNow,
                Id = invitationId
            });

            return rowsAffected > 0;
        }
    }
}