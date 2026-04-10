namespace backend.Infrastructure.Repositories
{
    using backend.Application.Interfaces;
    using backend.Domain.Entities;
    using backend.Infrastructure.Persistence;
    using Dapper;

    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _db;

        public UserRepository(IDbConnectionFactory db)
        {
            _db = db;
        }

        public async Task<Guid> Create(string username, string email, string passwordHash)
        {
            var sql = @"
                INSERT INTO users (username, email, password_hash, global_role)
                VALUES (@Username, @Email, @PasswordHash, 'user')
                RETURNING id;
            ";

            using var conn = _db.Create();

            return await conn.ExecuteScalarAsync<Guid>(sql, new
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash
            });
        }

        public async Task<User?> GetByEmail(string email)
        {
            var sql = @"
                SELECT 
                    id AS Id,
                    username AS Username,
                    email AS Email,
                    password_hash AS PasswordHash,
                    global_role AS GlobalRole,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM users
                WHERE email = @Email
            ";

            using var conn = _db.Create();

            return await conn.QueryFirstOrDefaultAsync<User>(sql, new
            {
                Email = email
            });
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            var sql = @"
                SELECT 
                    id AS Id,
                    username AS Username,
                    email AS Email,
                    password_hash AS PasswordHash,
                    global_role AS GlobalRole,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM users
                WHERE id = @Id
            ";

            using var conn = _db.Create();

            return await conn.QueryFirstOrDefaultAsync<User>(sql, new
            {
                Id = id
            });
        }
    }
}
