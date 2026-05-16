namespace backend.Application.DTOs.File
{
    public class ReconstructResponse
    {
        public Stream Stream { get; set; } = default!;
        public string FileName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
    }
}
