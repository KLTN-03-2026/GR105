namespace backend.Application.DTOs.Workspace
{
    public class GenerateInviteCodeResponse
    {
        public string InviteCode { get; set; } = default!;
        public bool InviteEnabled { get; set; }
    }
}