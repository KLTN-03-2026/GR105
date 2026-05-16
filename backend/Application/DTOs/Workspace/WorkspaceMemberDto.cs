namespace backend.Application.DTOs.Workspace
{
    public class WorkspaceMemberDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = default!;
        public string Role { get; set; } = default!;
        public DateTime JoinedAt { get; set; }
    }
}
