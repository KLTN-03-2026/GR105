using backend.Application.Interfaces;
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

        public async Task LogActivityAsync(Guid? userId, Guid? workspaceId, string action, string? entityType, Guid? entityId)
        {
            using var connection = _dbConnectionFactory.Create();

            // Ép kiểu action string sang activity_action enum trong PostgreSQL
            var sql = @"
                INSERT INTO activity_logs (user_id, workspace_id, action, entity_type, entity_id, created_at)
                VALUES (@UserId, @WorkspaceId, @Action::activity_action, @EntityType, @EntityId, @CreatedAt)";

            await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                WorkspaceId = workspaceId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                CreatedAt = DateTime.UtcNow
            });
        }

    }
}
