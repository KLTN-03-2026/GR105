using backend.API.Hubs;
using backend.Application.DTOs.Comment;
using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace backend.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/workspaces/{workspaceId}/files/{fileId}/comments")]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly IUserContext _userContext;
        private readonly IHubContext<WorkspaceHub> _hubContext;

        public CommentController(
            ICommentService commentService, 
            IUserContext userContext,
            IHubContext<WorkspaceHub> hubContext)
        {
            _commentService = commentService;
            _userContext = userContext;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetComments(
            Guid workspaceId, 
            Guid fileId, 
            [FromQuery] Guid? versionId, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            var (comments, totalCount) = await _commentService.GetCommentsAsync(workspaceId, fileId, versionId, userId, page, pageSize);

            return Ok(new
            {
                Data = comments,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateComment(
            Guid workspaceId, 
            Guid fileId, 
            [FromBody] CommentRequest request)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _commentService.CreateCommentAsync(workspaceId, fileId, userId, request);

            // Push realtime event
            await _hubContext.Clients.Group($"file_{fileId}").SendAsync("comment_created", response);

            return StatusCode(201, response);
        }

        [HttpPut("{commentId}")]
        public async Task<IActionResult> UpdateComment(
            Guid workspaceId, 
            Guid fileId, 
            Guid commentId, 
            [FromBody] CommentRequest request)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _commentService.UpdateCommentAsync(workspaceId, fileId, commentId, userId, request.Content);

            if (success)
            {
                var payload = new { 
                    Id = commentId, 
                    FileId = fileId, 
                    VersionId = request.VersionId, 
                    Content = request.Content, 
                    UpdatedAt = DateTime.UtcNow 
                };
                // Báo cập nhật cho client
                await _hubContext.Clients.Group($"file_{fileId}").SendAsync("comment_updated", payload);
                return Ok(new { message = "Comment updated successfully." });
            }

            return BadRequest(new { message = "Update failed." });
        }

        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment(
            Guid workspaceId, 
            Guid fileId, 
            Guid commentId)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            // Service trả về VersionId nếu xóa thành công để Controller gửi push Payload chuẩn
            var versionId = await _commentService.DeleteCommentAsync(workspaceId, fileId, commentId, userId);

            var payload = new
            {
                id = commentId,
                fileId = fileId,
                versionId = versionId
            };
            
            await _hubContext.Clients.Group($"file_{fileId}").SendAsync("comment_deleted", payload);
            
            return Ok(new { message = "Comment deleted successfully." });
        }
    }
}
