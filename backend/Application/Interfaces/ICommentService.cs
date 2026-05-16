using backend.Application.DTOs.Comment;

namespace backend.Application.Interfaces
{
    public interface ICommentService
    {
        Task<(IEnumerable<CommentResponse> Comments, int TotalCount)> GetCommentsAsync(Guid workspaceId, Guid fileId, Guid? versionId, Guid userId, int page, int pageSize);
        Task<CommentResponse> CreateCommentAsync(Guid workspaceId, Guid fileId, Guid userId, CommentRequest request);
        Task<bool> UpdateCommentAsync(Guid workspaceId, Guid fileId, Guid commentId, Guid userId, string newContent);
        Task<Guid?> DeleteCommentAsync(Guid workspaceId, Guid fileId, Guid commentId, Guid userId);
    }
}
