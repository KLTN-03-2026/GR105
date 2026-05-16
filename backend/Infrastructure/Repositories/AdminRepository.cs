using backend.Application.DTOs.Admin;
using backend.Application.Interfaces;
using backend.Infrastructure.Persistence;
using Dapper;

namespace backend.Infrastructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly IDbConnectionFactory _db;

        public AdminRepository(IDbConnectionFactory db)
        {
            _db = db;
        }

        // ==========================
        // USER MANAGEMENT
        // ==========================
        public async Task<IEnumerable<AdminUserResponse>> GetAllUsersAsync(string? search, int limit, int offset)
        {
            var sql = @"
                SELECT 
                    id AS Id,
                    username AS Username,
                    email AS Email,
                    global_role AS GlobalRole,
                    is_locked AS IsLocked,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM users
                WHERE (@Search IS NULL OR unaccent(username) ILIKE '%' || unaccent(@Search) || '%' OR email ILIKE '%' || @Search || '%')
                ORDER BY created_at DESC
                LIMIT @Limit OFFSET @Offset;
            ";

            using var conn = _db.Create();
            return await conn.QueryAsync<AdminUserResponse>(sql, new { Search = search, Limit = limit, Offset = offset });
        }

        public async Task<int> GetTotalUsersCountAsync(string? search)
        {
            var sql = @"
                SELECT COUNT(*)
                FROM users
                WHERE (@Search IS NULL OR unaccent(username) ILIKE '%' || unaccent(@Search) || '%' OR email ILIKE '%' || @Search || '%');
            ";
            using var conn = _db.Create();
            return await conn.ExecuteScalarAsync<int>(sql, new { Search = search });
        }

        public async Task<Guid> CreateUserAsync(string username, string email, string passwordHash, string globalRole)
        {
            var sql = @"
                INSERT INTO users (username, email, password_hash, global_role)
                VALUES (@Username, @Email, @PasswordHash, @GlobalRole)
                RETURNING id;
            ";
            using var conn = _db.Create();
            return await conn.ExecuteScalarAsync<Guid>(sql, new
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                GlobalRole = globalRole
            });
        }

        public async Task<bool> UpdateUserAsync(Guid userId, string username, string globalRole)
        {
            var sql = @"
                UPDATE users
                SET username = @Username, global_role = @GlobalRole, updated_at = NOW()
                WHERE id = @UserId;
            ";
            using var conn = _db.Create();
            return await conn.ExecuteAsync(sql, new { UserId = userId, Username = username, GlobalRole = globalRole }) > 0;
        }

        public async Task<bool> ToggleUserLockStatusAsync(Guid userId, bool isLocked)
        {
            var sql = @"
                UPDATE users
                SET is_locked = @IsLocked, updated_at = NOW()
                WHERE id = @UserId;
            ";
            using var conn = _db.Create();
            return await conn.ExecuteAsync(sql, new { UserId = userId, IsLocked = isLocked }) > 0;
        }

        // ==========================
        // WORKSPACE MANAGEMENT
        // ==========================
        public async Task<IEnumerable<AdminWorkspaceResponse>> GetAllWorkspacesAsync(int limit, int offset)
        {
            var sql = @"
                SELECT 
                    w.id AS Id,
                    w.name AS Name,
                    w.owner_user_id AS OwnerUserId,
                    u.email AS OwnerEmail,
                    w.created_at AS CreatedAt,
                    w.updated_at AS UpdatedAt,
                    (SELECT COUNT(*) FROM files f WHERE f.workspace_id = w.id AND f.deleted_at IS NULL) AS FileCount,
                    (SELECT COUNT(*) FROM workspace_users wu WHERE wu.workspace_id = w.id) AS MemberCount
                FROM workspaces w
                JOIN users u ON w.owner_user_id = u.id
                ORDER BY w.created_at DESC
                LIMIT @Limit OFFSET @Offset;
            ";
            using var conn = _db.Create();
            return await conn.QueryAsync<AdminWorkspaceResponse>(sql, new { Limit = limit, Offset = offset });
        }

        public async Task<int> GetTotalWorkspacesCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM workspaces;";
            using var conn = _db.Create();
            return await conn.ExecuteScalarAsync<int>(sql);
        }

        public async Task<AdminWorkspaceResponse?> GetWorkspaceByIdAsync(Guid workspaceId)
        {
            var sql = @"
                SELECT 
                    w.id AS Id,
                    w.name AS Name,
                    w.owner_user_id AS OwnerUserId,
                    u.email AS OwnerEmail,
                    w.created_at AS CreatedAt,
                    w.updated_at AS UpdatedAt,
                    (SELECT COUNT(*) FROM files f WHERE f.workspace_id = w.id AND f.deleted_at IS NULL) AS FileCount,
                    (SELECT COUNT(*) FROM workspace_users wu WHERE wu.workspace_id = w.id) AS MemberCount
                FROM workspaces w
                JOIN users u ON w.owner_user_id = u.id
                WHERE w.id = @WorkspaceId;
            ";
            using var conn = _db.Create();
            return await conn.QueryFirstOrDefaultAsync<AdminWorkspaceResponse>(sql, new { WorkspaceId = workspaceId });
        }

        public async Task<bool> DeleteWorkspaceAsync(Guid workspaceId)
        {
            // CASCADE is enabled on DB, so this hard delete will wipe related entities.
            var sql = "DELETE FROM workspaces WHERE id = @WorkspaceId;";
            using var conn = _db.Create();
            return await conn.ExecuteAsync(sql, new { WorkspaceId = workspaceId }) > 0;
        }

        // ==========================
        // SYSTEM LOGS
        // ==========================
        public async Task<IEnumerable<AdminActivityLogResponse>> GetAllActivityLogsAsync(Guid? userId, Guid? workspaceId, string? action, int limit, int offset)
        {
            var sql = @"
                SELECT 
                    l.id AS Id,
                    l.user_id AS UserId,
                    u.username AS Username,
                    l.workspace_id AS WorkspaceId,
                    l.action::text AS Action,
                    l.entity_type AS EntityType,
                    l.entity_id AS EntityId,
                    l.entity_name AS EntityName,
                    l.created_at AS CreatedAt
                FROM activity_logs l
                LEFT JOIN users u ON l.user_id = u.id
                WHERE (@UserId IS NULL OR l.user_id = @UserId)
                  AND (@WorkspaceId IS NULL OR l.workspace_id = @WorkspaceId)
                  AND (@Action IS NULL OR l.action::text = @Action)
                ORDER BY l.created_at DESC
                LIMIT @Limit OFFSET @Offset;
            ";
            using var conn = _db.Create();
            return await conn.QueryAsync<AdminActivityLogResponse>(sql, new
            {
                UserId = userId,
                WorkspaceId = workspaceId,
                Action = action,
                Limit = limit,
                Offset = offset
            });
        }

        public async Task<int> GetTotalActivityLogsCountAsync(Guid? userId, Guid? workspaceId, string? action)
        {
            var sql = @"
                SELECT COUNT(*)
                FROM activity_logs l
                WHERE (@UserId IS NULL OR l.user_id = @UserId)
                  AND (@WorkspaceId IS NULL OR l.workspace_id = @WorkspaceId)
                  AND (@Action IS NULL OR l.action::text = @Action);
            ";
            using var conn = _db.Create();
            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                UserId = userId,
                WorkspaceId = workspaceId,
                Action = action
            });
        }

        // ==========================
        // FEEDBACKS
        // ==========================
        public async Task<IEnumerable<AdminFeedbackResponse>> GetAllFeedbacksAsync(string? status, int limit, int offset)
        {
            var sql = @"
                SELECT 
                    f.id AS Id,
                    f.user_id AS UserId,
                    u.username AS Username,
                    u.email AS Email,
                    f.content AS Content,
                    f.status AS Status,
                    f.created_at AS CreatedAt
                FROM feedbacks f
                LEFT JOIN users u ON f.user_id = u.id
                WHERE (@Status IS NULL OR f.status = @Status)
                ORDER BY f.created_at DESC
                LIMIT @Limit OFFSET @Offset;
            ";
            using var conn = _db.Create();
            return await conn.QueryAsync<AdminFeedbackResponse>(sql, new { Status = status, Limit = limit, Offset = offset });
        }

        public async Task<int> GetTotalFeedbacksCountAsync(string? status)
        {
            var sql = @"
                SELECT COUNT(*)
                FROM feedbacks
                WHERE (@Status IS NULL OR status = @Status);
            ";
            using var conn = _db.Create();
            return await conn.ExecuteScalarAsync<int>(sql, new { Status = status });
        }

        public async Task<bool> UpdateFeedbackStatusAsync(Guid feedbackId, string status)
        {
            var sql = "UPDATE feedbacks SET status = @Status WHERE id = @FeedbackId;";
            using var conn = _db.Create();
            return await conn.ExecuteAsync(sql, new { FeedbackId = feedbackId, Status = status }) > 0;
        }

        // ==========================
        // PASSWORD RESET REQUESTS
        // ==========================
        public async Task<IEnumerable<AdminPasswordResetRequestResponse>> GetAllPasswordResetRequestsAsync(int limit, int offset)
        {
            var sql = @"
                SELECT 
                    p.id AS Id,
                    p.user_id AS UserId,
                    u.username AS Username,
                    u.email AS Email,
                    p.token AS Token,
                    p.expired_at AS ExpiredAt,
                    p.created_at AS CreatedAt
                FROM password_reset_requests p
                JOIN users u ON p.user_id = u.id
                ORDER BY p.created_at DESC
                LIMIT @Limit OFFSET @Offset;
            ";
            using var conn = _db.Create();
            return await conn.QueryAsync<AdminPasswordResetRequestResponse>(sql, new { Limit = limit, Offset = offset });
        }

        public async Task<int> GetTotalPasswordResetRequestsCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM password_reset_requests;";
            using var conn = _db.Create();
            return await conn.ExecuteScalarAsync<int>(sql);
        }
    }
}
