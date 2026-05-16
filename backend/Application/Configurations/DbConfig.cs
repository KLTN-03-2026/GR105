namespace backend.Application.Configurations
{
    public class DbConfig
    {
        public string Host { get; init; } = default!;
        public string Port { get; init; } = default!;
        public string Database { get; init; } = default!;
        public string Username { get; init; } = default!;
        public string Password { get; init; } = default!;

        public string BuildConnectionString()
        {
            return $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}";
        }
    }
}