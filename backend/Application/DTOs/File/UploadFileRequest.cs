namespace backend.Application.DTOs.File
{
    public class UploadFileRequest
    {
        public IFormFile File { get; set; } = default!;
        public string? Title { get; set; }
        public string? FolderPath { get; set; }

        // Base version để hệ thống nhận diện việc chống conflict
        // Nếu là lần đầu upload, BaseVersionId = null
        // Nếu là update, client phải gửi BaseVersionId (chính là version mới nhất nó đang biết)
        public Guid? BaseVersionId { get; set; }
    }
}
