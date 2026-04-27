using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IReviewService
{
    Task<ReviewResponse?> GetReviewByIdAsync(Guid reviewId);
    Task<ReviewIndexedResponse> GetReviewsAsync(ReviewFilterRequest filter);
    Task<ReviewResponse?> CreateReviewAsync(ReviewCreateRequest request);
    Task<ReviewResponse?> UpdateReviewAsync(ReviewUpdateRequest request);
    Task<bool> DeleteReviewAsync(Guid reviewId);
}
