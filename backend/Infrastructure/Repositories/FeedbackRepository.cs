using backend.Application.Interfaces;
using backend.Domain.Entities;
using backend.Infrastructure.Persistence;
using Dapper;

namespace backend.Infrastructure.Repositories
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public FeedbackRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<Feedback> CreateAsync(Feedback feedback)
        {
            using var connection = _dbConnectionFactory.Create();
            var sql = @"
                INSERT INTO feedbacks (user_id, content, status, created_at)
                VALUES (@UserId, @Content, @Status, @CreatedAt)
                RETURNING id, user_id as UserId, content, status, created_at as CreatedAt;";

            return await connection.QuerySingleAsync<Feedback>(sql, feedback);
        }
    }
}