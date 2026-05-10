namespace backend.Application.DTOs.File;

public class DiffLineDto
{
    public string Type { get; set; } = string.Empty; // "Inserted", "Deleted", "Unchanged"
    public string Text { get; set; } = string.Empty;
    public int? LineOld { get; set; }
    public int? LineNew { get; set; }
}
