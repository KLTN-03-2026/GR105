using System.ComponentModel.DataAnnotations;

namespace backend.Application.DTOs.Comment
{
    public class CommentRequest
    {
        [Required(ErrorMessage = "Nội dung bình luận không được để trống")]
        [MaxLength(2000, ErrorMessage = "Nội dung bình luận tối đa 2000 ký tự")]
        public string Content { get; set; } = default!;

        public Guid? VersionId { get; set; }
    }
}
