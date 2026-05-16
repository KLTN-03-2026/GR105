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
                    is_locked AS IsLocked,
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
                    is_locked AS IsLocked,
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

        public async Task<bool> UpdateProfileAsync(Guid id, string username)
        {
            var sql = @"
                UPDATE users
                SET username = @Username, updated_at = @UpdatedAt
                WHERE id = @Id;
            ";

            using var conn = _db.Create();
            var rows = await conn.ExecuteAsync(sql, new
            {
                Id = id,
                Username = username,
                UpdatedAt = DateTime.UtcNow
            });
            return rows > 0;
        }

        public async Task<bool> UpdatePasswordAsync(Guid id, string newPasswordHash)
        {
            var sql = @"
                UPDATE users
                SET password_hash = @PasswordHash, updated_at = @UpdatedAt
                WHERE id = @Id;
            ";

            using var conn = _db.Create();
            var rows = await conn.ExecuteAsync(sql, new
            {
                Id = id,
                PasswordHash = newPasswordHash,
                UpdatedAt = DateTime.UtcNow
            });
            return rows > 0;
        }

        public async Task CreatePasswordResetTokenAsync(Guid userId, string token, DateTime expiredAt)
        {
            var sql = @"
                INSERT INTO password_reset_requests (user_id, token, expired_at)
                VALUES (@UserId, @Token, @ExpiredAt);
            ";
            using var conn = _db.Create();
            await conn.ExecuteAsync(sql, new { UserId = userId, Token = token, ExpiredAt = expiredAt });
        }

        public async Task<Guid?> GetUserIdByValidResetTokenAsync(string token)
        {
            var sql = @"
                SELECT user_id 
                FROM password_reset_requests
                WHERE token = @Token AND expired_at > NOW();
            ";
            using var conn = _db.Create();
            return await conn.QuerySingleOrDefaultAsync<Guid?>(sql, new { Token = token });
        }

        public async Task DeletePasswordResetTokenAsync(string token)
        {
            var sql = "DELETE FROM password_reset_requests WHERE token = @Token;";
            using var conn = _db.Create();
            await conn.ExecuteAsync(sql, new { Token = token });
        }
    }
}
