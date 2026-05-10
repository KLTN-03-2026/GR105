using backend.Application.DTOs.File;
using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/workspaces/{workspaceId}/files")]
public class FileController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly IMediaService _mediaService;
    private readonly IFileSearchService _fileSearchService;
    private readonly IUserContext _userContext;

    public FileController(IFileService fileService, IMediaService mediaService, IFileSearchService fileSearchService, IUserContext userContext)
    {
        _fileService = fileService;
        _mediaService = mediaService;
        _fileSearchService = fileSearchService;
        _userContext = userContext;
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchFiles([FromRoute] Guid workspaceId, [FromQuery] string q)
    {
        var result = await _fileSearchService.SearchAsync(workspaceId, q);
        return Ok(result);
    }

    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue, MultipartHeadersLengthLimit = int.MaxValue)]
    public async Task<IActionResult> UploadFile([FromRoute] Guid workspaceId, [FromForm] UploadFileRequest request)
    {
        var userId = _userContext.UserId;
        var response = await _fileService.UploadFileAsync(workspaceId, userId, request);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetFiles([FromRoute] Guid workspaceId, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
    {
        var userId = _userContext.UserId;
        var response = await _fileService.GetFilesByWorkspaceAsync(userId, workspaceId, limit, offset);
        return Ok(response);
    }

    [HttpGet("trash")]
    public async Task<IActionResult> GetTrashFiles([FromRoute] Guid workspaceId, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
    {
        var userId = _userContext.UserId;
        var response = await _fileService.GetTrashFilesAsync(userId, workspaceId, limit, offset);
        return Ok(response);
    }

    [HttpDelete("{fileId}")]
    public async Task<IActionResult> SoftDeleteFile([FromRoute] Guid workspaceId, [FromRoute] Guid fileId)
    {
        var userId = _userContext.UserId;
        await _fileService.SoftDeleteFileAsync(userId, fileId);
        return NoContent();
    }

    [HttpPost("{fileId}/restore")]
    public async Task<IActionResult> RestoreFile([FromRoute] Guid workspaceId, [FromRoute] Guid fileId)
    {
        var userId = _userContext.UserId;
        await _fileService.RestoreFileAsync(userId, fileId);
        return Ok(new { message = "File restored successfully." });
    }

    [HttpGet("{fileId}/versions")]
    public async Task<IActionResult> GetFileVersions([FromRoute] Guid workspaceId, [FromRoute] Guid fileId)
    {
        var userId = _userContext.UserId;
        var response = await _fileService.GetFileVersionsAsync(userId, fileId);
        return Ok(response);
    }

    [HttpGet("{fileId}/download")]
    public async Task<IActionResult> DownloadFile([FromRoute] Guid workspaceId, [FromRoute] Guid fileId, [FromQuery] Guid? versionId)
    {
        var userId = _userContext.UserId;
        var response = await _fileService.DownloadFileAsync(userId, fileId, versionId);
        return File(response.Stream, response.ContentType, response.FileName);
    }

    [HttpGet("{fileId}/versions/{versionId}/preview")]
    public async Task<IActionResult> GetPreview([FromRoute] Guid workspaceId, [FromRoute] Guid fileId, [FromRoute] Guid versionId)
    {
        var userId = _userContext.UserId;
        var response = await _fileService.GetPreviewAsync(userId, fileId, versionId);
        return Ok(response);
    }

    [HttpGet("{fileId}/diff")]
    public async Task<IActionResult> GetDiff([FromRoute] Guid workspaceId, [FromRoute] Guid fileId, [FromQuery] Guid baseVersionId, [FromQuery] Guid targetVersionId)
    {
        var userId = _userContext.UserId;
        var response = await _fileService.GetDiffAsync(userId, fileId, baseVersionId, targetVersionId);
        return Ok(response);
    }

    [HttpGet("{fileId}/versions/{versionId}/view")]
    public async Task<IActionResult> ViewFile([FromRoute] Guid workspaceId, [FromRoute] Guid fileId, [FromRoute] Guid versionId)
    {
        var userId = _userContext.UserId;
        var result = await _mediaService.GetMediaAsync(userId, fileId, versionId);

        if (result.Type == MediaType.Error) 
            return BadRequest(new { message = result.Error, canDownload = true });
        
        if (result.Type == MediaType.Text) 
            return Ok(new { content = result.TextContent, fileType = result.FileExtension });
        
        if (result.Type == MediaType.Stream && result.Stream != null) 
            return File(result.Stream, result.ContentType ?? "application/octet-stream", enableRangeProcessing: true);
        
        return BadRequest(new { message = "Unknown error occurred while processing media." });
    }
}
