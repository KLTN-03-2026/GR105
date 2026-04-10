using backend.Application.Interfaces;
using backend.Domain.Entities;
using backend.Infrastructure.Persistence;
using Dapper;

namespace backend.Infrastructure.Repositories
{
    public class WorkspaceRepository : IWorkspaceRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public WorkspaceRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

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
                    RETURNING id, name, owner_user_id as OwnerUserId, created_at as CreatedAt, updated_at as UpdatedAt;";

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
                SELECT id, name, owner_user_id AS OwnerUserId, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM workspaces
                WHERE id = @Id";
            return await connection.QuerySingleOrDefaultAsync<Workspace>(sql, new { Id = id });
        }

        public async Task<IEnumerable<Workspace>> GetOwnedAsync(Guid ownerUserId)
        {
            using var connection = _dbConnectionFactory.Create();
            var sql = @"
                SELECT id, name, owner_user_id AS OwnerUserId, created_at AS CreatedAt, updated_at AS UpdatedAt
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
            // Vì có ON DELETE CASCADE, chỉ cần xoá ở bảng workspaces
            var sql = "DELETE FROM workspaces WHERE id = @Id";
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }
    }
}
