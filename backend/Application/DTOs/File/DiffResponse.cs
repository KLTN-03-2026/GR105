namespace backend.Application.DTOs.File;

public class DiffResponse
{
    public string Status { get; set; } = "Success"; // "Success", "NotSupported"
    public string Message { get; set; } = string.Empty;
    public IEnumerable<DiffLineDto> Lines { get; set; } = new List<DiffLineDto>();
}
