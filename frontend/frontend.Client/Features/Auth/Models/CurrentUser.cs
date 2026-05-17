namespace frontend.Client.Features.Auth.Models
{
    public sealed class CurrentUser
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string GlobalRole { get; set; } = string.Empty;
    }
}

