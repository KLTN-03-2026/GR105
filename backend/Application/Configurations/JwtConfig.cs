namespace backend.Application.Configurations
{
    public class JwtConfig
    {
        public string Secret { get; init; } = default!;
        public string Issuer { get; init; } = default!;
        public string Audience { get; init; } = default!;
        public int ExpireMinutes { get; init; }
    }
}
