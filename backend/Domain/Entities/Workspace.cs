namespace backend.Domain.Entities
{
    public class Workspace
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public Guid OwnerUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
