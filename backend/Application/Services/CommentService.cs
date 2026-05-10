using backend.Application.Common.Exceptions;
using backend.Application.DTOs.Comment;
using backend.Application.Interfaces;
using backend.Domain.Entities;

namespace backend.Application.Services
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly IUserRepository _userRepository;

        public CommentService(
            ICommentRepository commentRepository,
            IWorkspaceRepository workspaceRepository,
            IFileRepository fileRepository,
            IActivityLogRepository activityLogRepository,
            IUserRepository userRepository)
        {
            _commentRepository = commentRepository;
            _workspaceRepository = workspaceRepository;
            _fileRepository = fileRepository;
            _activityLogRepository = activityLogRepository;
            _userRepository = userRepository;
        }

        private async Task CheckWorkspaceAccessAsync(Guid workspaceId, Guid userId)
        {
            bool isMember = await _workspaceRepository.IsUserInWorkspaceAsync(userId, workspaceId);
            if (!isMember)
            {
                throw new ForbiddenException("Bạn không có quyền truy cập vào Workspace này.");
            }
        }

        private async Task ValidateFileAndVersionAsync(Guid workspaceId, Guid fileId, Guid? versionId)
        {
            var file = await _fileRepository.GetFileByIdAsync(fileId);
            if (file == null || file.DeletedAt != null)
            {
                throw new NotFoundException("Không tìm thấy File hoặc File đã bị xóa.");
            }

            if (file.WorkspaceId != workspaceId)
            {
                throw new ForbiddenException("File không thuộc Workspace này.");
            }

            if (versionId.HasValue)
            {
                var version = await _fileRepository.GetVersionByIdAsync(versionId.Value);
                if (version == null)
                {
                    throw new NotFoundException("Không tìm thấy Version.");
                }

                if (version.FileId != fileId)
                {
                    throw new ValidationException("Version không thuộc File này.");
                }
            }
        }

        public async Task<(IEnumerable<CommentResponse> Comments, int TotalCount)> GetCommentsAsync(Guid workspaceId, Guid fileId, Guid? versionId, Guid userId, int page, int pageSize)
        {
            await CheckWorkspaceAccessAsync(workspaceId, userId);
            await ValidateFileAndVersionAsync(workspaceId, fileId, versionId);

            return await _commentRepository.GetCommentsAsync(fileId, versionId, page, pageSize);
        }

        public async Task<CommentResponse> CreateCommentAsync(Guid workspaceId, Guid fileId, Guid userId, CommentRequest request)
        {
            await CheckWorkspaceAccessAsync(workspaceId, userId);
            await ValidateFileAndVersionAsync(workspaceId, fileId, request.VersionId);

            var comment = new Comment
            {
                FileId = fileId,
                VersionId = request.VersionId,
                UserId = userId,
                Content = request.Content
            };

            var createdComment = await _commentRepository.CreateAsync(comment);

            // Ghi Log Activity (UC9)
            await _activityLogRepository.LogActivityAsync(userId, workspaceId, "COMMENT", "File", fileId);

            var user = await _userRepository.GetByIdAsync(userId);

            return new CommentResponse
            {
                Id = createdComment.Id,
                FileId = createdComment.FileId,
                VersionId = createdComment.VersionId,
                Content = createdComment.Content,
                CreatedAt = createdComment.CreatedAt,
                UpdatedAt = createdComment.UpdatedAt,
                UserId = userId,
                Username = user?.Username ?? "Unknown"
            };
        }

        public async Task<bool> UpdateCommentAsync(Guid workspaceId, Guid fileId, Guid commentId, Guid userId, string newContent)
        {
            await CheckWorkspaceAccessAsync(workspaceId, userId);
            
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null)
            {
                throw new NotFoundException("Không tìm thấy bình luận.");
            }

            if (comment.FileId != fileId)
            {
                throw new ValidationException("Bình luận không thuộc File này.");
            }

            // Chỉ tác giả mới được quyền sửa bình luận
            if (comment.UserId != userId)
            {
                throw new ForbiddenException("Bạn không có quyền sửa bình luận của người khác.");
            }

            return await _commentRepository.UpdateAsync(commentId, newContent);
        }

        public async Task<Guid?> DeleteCommentAsync(Guid workspaceId, Guid fileId, Guid commentId, Guid userId)
        {
            await CheckWorkspaceAccessAsync(workspaceId, userId);
            
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null)
            {
                throw new NotFoundException("Không tìm thấy bình luận.");
            }

            if (comment.FileId != fileId)
            {
                throw new ValidationException("Bình luận không thuộc File này.");
            }

            // Phân quyền Xóa: Chủ bình luận HOẶC Chủ Workspace
            bool canDelete = comment.UserId == userId;
            if (!canDelete)
            {
                var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
                if (workspace != null && workspace.OwnerUserId == userId)
                {
                    canDelete = true;
                }
            }

            if (!canDelete)
            {
                throw new ForbiddenException("Bạn không có quyền xóa bình luận này.");
            }

            var success = await _commentRepository.SoftDeleteAsync(commentId);
            if (success)
            {
                // Optional: Nếu DB đã có enum DELETE_COMMENT, mở dòng này.
                // await _activityLogRepository.LogActivityAsync(userId, workspaceId, "DELETE_COMMENT", "File", fileId);
                return comment.VersionId;
            }
            
            throw new Exception("Có lỗi xảy ra khi xóa bình luận.");
        }
    }
}
