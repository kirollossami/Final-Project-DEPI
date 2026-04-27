using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IHousingUnitRepository _housingUnitRepository;

    public ReviewService(IReviewRepository reviewRepository, IHousingUnitRepository housingUnitRepository)
    {
        _reviewRepository = reviewRepository;
        _housingUnitRepository = housingUnitRepository;
    }

    public async Task<ReviewResponse?> GetReviewByIdAsync(Guid reviewId)
    {
        var review = await _reviewRepository.GetAsync(reviewId);
        if (review == null) return null;

        return new ReviewResponse
        {
            ReviewId = review.ReviewId,
            StudentId = review.StudentId,
            HousingUnitId = review.HousingUnitId,
            Rating = review.Rating,
            Comment = review.Comment,
            ReviewDate = review.ReviewDate
        };
    }

    public async Task<ReviewIndexedResponse> GetReviewsAsync(ReviewFilterRequest filter)
    {
        var query = _reviewRepository.GetAll().AsQueryable();

        if (filter.StudentId.HasValue)
        {
            query = query.Where(r => r.StudentId == filter.StudentId.Value);
        }

        if (filter.HousingUnitId.HasValue)
        {
            query = query.Where(r => r.HousingUnitId == filter.HousingUnitId.Value);
        }

        if (filter.MinRating.HasValue)
        {
            query = query.Where(r => r.Rating >= filter.MinRating.Value);
        }

        if (filter.MaxRating.HasValue)
        {
            query = query.Where(r => r.Rating <= filter.MaxRating.Value);
        }

        var totalCount = await query.CountAsync();
        var reviews = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new ReviewIndexedResponse
        {
            Records = reviews.Select(r => new ReviewResponse
            {
                ReviewId = r.ReviewId,
                StudentId = r.StudentId,
                HousingUnitId = r.HousingUnitId,
                Rating = r.Rating,
                Comment = r.Comment,
                ReviewDate = r.ReviewDate
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
        };
    }

    public async Task<ReviewResponse?> CreateReviewAsync(ReviewCreateRequest request)
    {
        var review = new Domain.Entities.Review
        {
            ReviewId = Guid.NewGuid(),
            StudentId = request.StudentId,
            HousingUnitId = request.HousingUnitId,
            Rating = request.Rating,
            Comment = request.Comment,
            ReviewDate = DateTime.UtcNow
        };

        await _reviewRepository.Insert(review);
        await _reviewRepository.CommitAsync();

        await UpdateHousingUnitRatingAsync(review.HousingUnitId);

        return new ReviewResponse
        {
            ReviewId = review.ReviewId,
            StudentId = review.StudentId,
            HousingUnitId = review.HousingUnitId,
            Rating = review.Rating,
            Comment = review.Comment,
            ReviewDate = review.ReviewDate
        };
    }

    public async Task<ReviewResponse?> UpdateReviewAsync(ReviewUpdateRequest request)
    {
        var review = await _reviewRepository.GetAsync(request.ReviewId);
        if (review == null) return null;

        if (request.Rating.HasValue)
        {
            review.Rating = request.Rating.Value;
        }

        if (request.Comment != null)
        {
            review.Comment = request.Comment;
        }

        await _reviewRepository.Update(review);
        await _reviewRepository.CommitAsync();

        await UpdateHousingUnitRatingAsync(review.HousingUnitId);

        return new ReviewResponse
        {
            ReviewId = review.ReviewId,
            StudentId = review.StudentId,
            HousingUnitId = review.HousingUnitId,
            Rating = review.Rating,
            Comment = review.Comment,
            ReviewDate = review.ReviewDate
        };
    }

    public async Task<bool> DeleteReviewAsync(Guid reviewId)
    {
        var review = await _reviewRepository.GetAsync(reviewId);
        if (review == null) return false;

        await _reviewRepository.Delete(review);
        await _reviewRepository.CommitAsync();

        await UpdateHousingUnitRatingAsync(review.HousingUnitId);

        return true;
    }

    private async Task UpdateHousingUnitRatingAsync(Guid housingUnitId)
    {
        var housingUnit = await _housingUnitRepository.GetAsync(housingUnitId);
        if (housingUnit == null) return;

        var reviews = await _reviewRepository.GetAll()
            .Where(r => r.HousingUnitId == housingUnitId)
            .ToListAsync();

        if (reviews.Any())
        {
            housingUnit.AverageRating = reviews.Average(r => r.Rating);
            housingUnit.ReviewCount = reviews.Count;
        }
        else
        {
            housingUnit.AverageRating = 0;
            housingUnit.ReviewCount = 0;
        }

        _housingUnitRepository.Update(housingUnit);
        await _housingUnitRepository.CommitAsync();
    }
}
