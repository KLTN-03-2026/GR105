namespace backend.Infrastructure.Persistence
{
    using backend.Application.Configurations;
    using Npgsql;
    using System.Data;
    public interface IDbConnectionFactory
    {
        IDbConnection Create();
    }
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _conn;

        public DbConnectionFactory(DbConfig config)
        {
            _conn = config.BuildConnectionString();
        }

        public IDbConnection Create()
        {
            return new NpgsqlConnection(_conn);
        }
    }
}
