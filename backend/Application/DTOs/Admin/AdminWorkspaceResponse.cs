namespace backend.Application.DTOs.Admin
{
    public class AdminWorkspaceResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid OwnerUserId { get; set; }
        public string OwnerEmail { get; set; } = string.Empty;
        public int FileCount { get; set; }
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
