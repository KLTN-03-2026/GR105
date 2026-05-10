using backend.Domain.Entities;

namespace backend.Application.Interfaces
{
    public interface IFeedbackRepository
    {
        Task<Feedback> CreateAsync(Feedback feedback);
    }
}