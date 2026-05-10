using backend.Application.DTOs.Comment;

namespace backend.Application.Interfaces
{
    public interface ICommentRepository
    {
        Task<backend.Domain.Entities.Comment> CreateAsync(backend.Domain.Entities.Comment comment);
        Task<backend.Domain.Entities.Comment?> GetByIdAsync(Guid id);
        
        // Trả về danh sách bình luận (phân trang), kèm theo Username của tác giả
        Task<(IEnumerable<CommentResponse> Comments, int TotalCount)> GetCommentsAsync(Guid fileId, Guid? versionId, int page, int pageSize);
        
        Task<bool> UpdateAsync(Guid id, string content);
        Task<bool> SoftDeleteAsync(Guid id);
    }
}
