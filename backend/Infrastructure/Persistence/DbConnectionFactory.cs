using backend.Application.Configurations;
using backend.Application.Interfaces;
using Npgsql;
using System.Data;
using Microsoft.Extensions.Logging;

namespace backend.Infrastructure.Persistence
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _conn;
        private readonly ILogger<DbConnectionFactory> _logger;

        public DbConnectionFactory(DbConfig config, ILogger<DbConnectionFactory> logger)
        {
            _logger = logger;
            _conn = config.BuildConnectionString();
        }

        public IDbConnection Create()
        {
            try
            {
                var connection = new NpgsqlConnection(_conn);
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create NpgsqlConnection with connection string: {Conn}", MaskPassword(_conn));
                throw;
            }
        }

        private string MaskPassword(string connString)
        {
            if (string.IsNullOrEmpty(connString)) return "";
            var parts = connString.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
                {
                    parts[i] = "Password=********";
                }
            }
            return string.Join(";", parts);
        }
    }
}
