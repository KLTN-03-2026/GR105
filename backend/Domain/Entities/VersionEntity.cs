namespace backend.Domain.Entities
{
    public class VersionEntity
    {
        public Guid Id { get; set; }
        public Guid FileId { get; set; }
        public int VersionNumber { get; set; }
        public string StoragePath { get; set; } = default!;
        public bool IsFull { get; set; }
        public Guid? BaseVersionId { get; set; }
        public long FileSize { get; set; }
        public string? Checksum { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }
}
