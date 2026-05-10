namespace backend.Application.DTOs.File;

public enum MediaType
{
    Stream,
    Text,
    Error
}

public class MediaResult
{
    public MediaType Type { get; set; }
    public Stream? Stream { get; set; }
    public string? ContentType { get; set; }
    public string? TextContent { get; set; }
    public string? Error { get; set; }
    public string? FileExtension { get; set; }
}
