namespace backend.Application.DTOs.Workspace
{
    public class AddWorkspaceMemberRequest
    {
        public string Email { get; set; } = default!;
        public string Role { get; set; } = "member";
    }
}
