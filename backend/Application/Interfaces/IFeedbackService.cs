using backend.Application.DTOs.Feedback;

namespace backend.Application.Interfaces
{
    public interface IFeedbackService
    {
        Task<bool> CreateFeedbackAsync(Guid userId, CreateFeedbackRequest request);
    }
}