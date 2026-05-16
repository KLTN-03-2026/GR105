namespace backend.Application.DTOs.File
{
    public class VersionResponse
    {
        public Guid Id { get; set; }
        public Guid FileId { get; set; }
        public int VersionNumber { get; set; }
        public bool IsFull { get; set; }
        public long FileSize { get; set; }
        public string? Checksum { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }
}
