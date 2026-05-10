using backend.Application.Interfaces;
using backend.Application.DTOs.Workspace;
using backend.Infrastructure.Persistence;
using Dapper;

namespace backend.Infrastructure.Repositories
{
    public class ActivityLogRepository : IActivityLogRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public ActivityLogRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task LogActivityAsync(Guid? userId, Guid? workspaceId, string action, string? entityType, Guid? entityId, string? entityName = null)
        {
            using var connection = _dbConnectionFactory.Create();

            // Ép kiểu action string sang activity_action enum trong PostgreSQL
            var sql = @"
                INSERT INTO activity_logs (user_id, workspace_id, action, entity_type, entity_id, entity_name, created_at)
                VALUES (@UserId, @WorkspaceId, @Action::activity_action, @EntityType, @EntityId, @EntityName, @CreatedAt)";

            await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                WorkspaceId = workspaceId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task<IEnumerable<ActivityLogResponseDto>> GetWorkspaceLogsAsync(Guid workspaceId, int limit = 50, int offset = 0)
        {
            using var connection = _dbConnectionFactory.Create();
            var sql = @"
                SELECT 
                    l.id as ""Id"",
                    l.action as ""Action"",
                    l.entity_type as ""EntityType"",
                    l.entity_id as ""EntityId"",
                    l.entity_name as ""EntityName"",
                    l.created_at as ""CreatedAt"",
                    u.id as ""UserId"",
                    u.username as ""Username"",
                    u.email as ""Email""
                FROM activity_logs l
                LEFT JOIN users u ON l.user_id = u.id
                WHERE l.workspace_id = @WorkspaceId
                ORDER BY l.created_at DESC
                LIMIT @Limit OFFSET @Offset;";

            return await connection.QueryAsync<ActivityLogResponseDto>(sql, new { WorkspaceId = workspaceId, Limit = limit, Offset = offset });
        }
    }
}
