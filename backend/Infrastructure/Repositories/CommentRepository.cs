using backend.Application.DTOs.Comment;
using backend.Application.Interfaces;
using backend.Domain.Entities;
using Dapper;

namespace backend.Infrastructure.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly IDbSession _session;

        public CommentRepository(IDbSession session)
        {
            _session = session;
        }

        public async Task<Comment> CreateAsync(Comment comment)
        {
            const string sql = @"
                INSERT INTO comments (file_id, version_id, user_id, content, created_at, updated_at)
                VALUES (@FileId, @VersionId, @UserId, @Content, @CreatedAt, @UpdatedAt)
                RETURNING id;
            ";

            // Có thể bỏ qua CreatedAt/UpdatedAt ở đây nếu dùng DB DEFAULT NOW(),
            // nhưng set ở application layer sẽ an toàn hơn khi trả về Entity.
            comment.CreatedAt = DateTime.UtcNow;
            comment.UpdatedAt = DateTime.UtcNow;

            var id = await _session.Connection.ExecuteScalarAsync<Guid>(sql, comment, _session.Transaction);
            comment.Id = id;
            return comment;
        }

        public async Task<Comment?> GetByIdAsync(Guid id)
        {
            const string sql = "SELECT * FROM comments WHERE id = @Id AND deleted_at IS NULL";
            return await _session.Connection.QuerySingleOrDefaultAsync<Comment>(sql, new { Id = id }, _session.Transaction);
        }

        public async Task<(IEnumerable<CommentResponse> Comments, int TotalCount)> GetCommentsAsync(Guid fileId, Guid? versionId, int page, int pageSize)
        {
            int offset = (page - 1) * pageSize;
            
            // Xây dựng câu SQL cụ thể cho 2 case
            string baseCondition = "c.file_id = @FileId AND c.deleted_at IS NULL";
            string versionCondition = versionId.HasValue ? " AND c.version_id = @VersionId" : " AND c.version_id IS NULL";
            string fullCondition = baseCondition + versionCondition;

            string countSql = $@"
                SELECT COUNT(*) 
                FROM comments c 
                WHERE {fullCondition}
            ";

            string dataSql = $@"
                SELECT 
                    c.id as Id,
                    c.file_id as FileId,
                    c.version_id as VersionId,
                    c.content as Content,
                    c.created_at as CreatedAt,
                    c.updated_at as UpdatedAt,
                    u.id as UserId,
                    u.username as Username
                FROM comments c
                LEFT JOIN users u ON c.user_id = u.id
                WHERE {fullCondition}
                ORDER BY c.created_at ASC, c.id ASC
                OFFSET @Offset LIMIT @Limit
            ";

            var totalCount = await _session.Connection.ExecuteScalarAsync<int>(countSql, new { FileId = fileId, VersionId = versionId }, _session.Transaction);
            var comments = await _session.Connection.QueryAsync<CommentResponse>(dataSql, new { FileId = fileId, VersionId = versionId, Offset = offset, Limit = pageSize }, _session.Transaction);

            return (comments, totalCount);
        }

        public async Task<bool> UpdateAsync(Guid id, string content)
        {
            const string sql = @"
                UPDATE comments 
                SET content = @Content, updated_at = NOW() 
                WHERE id = @Id AND deleted_at IS NULL
            ";
            var rows = await _session.Connection.ExecuteAsync(sql, new { Id = id, Content = content }, _session.Transaction);
            return rows > 0;
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            const string sql = @"
                UPDATE comments 
                SET deleted_at = NOW() 
                WHERE id = @Id AND deleted_at IS NULL
            ";
            var rows = await _session.Connection.ExecuteAsync(sql, new { Id = id }, _session.Transaction);
            return rows > 0;
        }
    }
}
