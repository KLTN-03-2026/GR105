namespace backend.Application.DTOs.File;

public class PreviewResponse
{
    public string Content { get; set; } = string.Empty;
    public bool IsTruncated { get; set; }
    public string FileType { get; set; } = "unknown"; // "text", "docx", "binary"
}
