using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace StudentHousingAPI.Controllers;

/// <summary>
/// Controller for managing property reviews
/// Only allows students to review properties they have bookings with
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewController : BaseController
{
    private readonly IReviewService _reviewService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ReviewController> _logger;

    public ReviewController(
        IReviewService reviewService,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILogger<ReviewController> logger)
    {
        _reviewService = reviewService;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all reviews for the current student
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not identified" });
            }

            // Get student ID from user ID
            var studentId = await GetStudentIdFromUserId(userId);
            if (studentId == null)
            {
                return BadRequest(new { Message = "Student profile not found" });
            }

            var filter = new ReviewFilterRequest
            {
                StudentId = studentId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var reviews = await _reviewService.GetReviewsAsync(filter);

            _logger.LogInformation($"Student {studentId} retrieved {reviews.Records.Count()} reviews");

            return Ok(new
            {
                Success = true,
                Data = reviews.Records,
                TotalRecords = reviews.TotalRecords,
                PageIndex = reviews.PageIndex,
                PageSize = reviews.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving student reviews");
            return BadRequest(new { Message = "Error retrieving reviews", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get reviews for a specific housing unit (public)
    /// </summary>
    [HttpGet("housing-unit/{housingUnitId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviewsByHousingUnit(
        Guid housingUnitId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var filter = new ReviewFilterRequest
            {
                HousingUnitId = housingUnitId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var reviews = await _reviewService.GetReviewsAsync(filter);

            return Ok(new
            {
                Success = true,
                Data = reviews.Records,
                TotalRecords = reviews.TotalRecords,
                PageIndex = reviews.PageIndex,
                PageSize = reviews.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving housing unit reviews");
            return BadRequest(new { Message = "Error retrieving reviews", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific review by ID
    /// </summary>
    [HttpGet("{reviewId}")]
    public async Task<IActionResult> GetReviewById(Guid reviewId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var review = await _reviewService.GetReviewByIdAsync(reviewId);

            if (review == null)
            {
                return NotFound(new { Message = "Review not found" });
            }

            // Verify ownership (students can only see their own reviews, admins can see all)
            var studentId = await GetStudentIdFromUserId(userId ?? "");
            if (review.StudentId != studentId && !HasRole("Admin"))
            {
                return Forbid("You do not have permission to view this review");
            }

            return Ok(new
            {
                Success = true,
                Data = review
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving review");
            return BadRequest(new { Message = "Error retrieving review", Error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new review
    /// Only allows students to review properties they have bookings with
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] ReviewCreateRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not identified" });
            }

            // Get student ID from user ID
            var studentId = await GetStudentIdFromUserId(userId);
            if (studentId == null)
            {
                return BadRequest(new { Message = "Student profile not found" });
            }

            // Validate that student has a booking with this housing unit
            var hasBooking = await HasBookingWithHousingUnit(studentId.Value, request.HousingUnitId);
            if (!hasBooking)
            {
                return BadRequest(new { 
                    Message = "You can only review properties you have booked" 
                });
            }

            // Check if student already reviewed this housing unit
            var hasExistingReview = await HasExistingReview(studentId.Value, request.HousingUnitId);
            if (hasExistingReview)
            {
                return BadRequest(new { 
                    Message = "You have already reviewed this property. You can update your existing review instead." 
                });
            }

            // Set the student ID to the authenticated student
            request.StudentId = studentId.Value;

            var review = await _reviewService.CreateReviewAsync(request);

            if (review == null)
            {
                return BadRequest(new { Message = "Failed to create review" });
            }

            // Send notification to landlord about the review
            await SendReviewNotificationToLandlord(review.ReviewId, request.HousingUnitId, request.Rating);

            _logger.LogInformation($"Review {review.ReviewId} created by student {studentId}");

            return Ok(new
            {
                Success = true,
                Data = review,
                Message = "Review created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review");
            return BadRequest(new { Message = "Error creating review", Error = ex.Message });
        }
    }

    /// <summary>
    /// Update a review
    /// Only allows students to update their own reviews
    /// </summary>
    [HttpPut("{reviewId}")]
    public async Task<IActionResult> UpdateReview(Guid reviewId, [FromBody] ReviewUpdateRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var existingReview = await _reviewService.GetReviewByIdAsync(reviewId);

            if (existingReview == null)
            {
                return NotFound(new { Message = "Review not found" });
            }

            // Verify ownership (students can only update their own reviews)
            var studentId = await GetStudentIdFromUserId(userId ?? "");
            if (existingReview.StudentId != studentId && !HasRole("Admin"))
            {
                return Forbid("You do not have permission to update this review");
            }

            request.ReviewId = reviewId;
            var review = await _reviewService.UpdateReviewAsync(request);

            if (review == null)
            {
                return BadRequest(new { Message = "Failed to update review" });
            }

            await SendReviewNotificationToLandlord(review.ReviewId, review.HousingUnitId, review.Rating);

            _logger.LogInformation($"Review {reviewId} updated by student {studentId}");

            return Ok(new
            {
                Success = true,
                Data = review,
                Message = "Review updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating review");
            return BadRequest(new { Message = "Error updating review", Error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a review
    /// Only allows students to delete their own reviews
    /// </summary>
    [HttpDelete("{reviewId}")]
    public async Task<IActionResult> DeleteReview(Guid reviewId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var existingReview = await _reviewService.GetReviewByIdAsync(reviewId);

            if (existingReview == null)
            {
                return NotFound(new { Message = "Review not found" });
            }

            // Verify ownership (students can only delete their own reviews)
            var studentId = await GetStudentIdFromUserId(userId ?? "");
            if (existingReview.StudentId != studentId && !HasRole("Admin"))
            {
                return Forbid("You do not have permission to delete this review");
            }

            var result = await _reviewService.DeleteReviewAsync(reviewId);

            if (!result)
            {
                return BadRequest(new { Message = "Failed to delete review" });
            }

            try
            {
                var unit = await _unitOfWork.HousingUnits.GetAsync(existingReview.HousingUnitId);
                if (unit != null)
                {
                    var landlord = await _unitOfWork.LandLords.GetAsync(unit.LandLordId);
                    if (landlord?.UserId != null)
                        await _notificationService.SendRealTimeNotificationAsync(landlord.UserId, "A review has been deleted from your property.", NotificationTypes.ReviewDeleted);
                }
            }
            catch { }

            _logger.LogInformation($"Review {reviewId} deleted by student {studentId}");

            return Ok(new
            {
                Success = true,
                Message = "Review deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review");
            return BadRequest(new { Message = "Error deleting review", Error = ex.Message });
        }
    }

    // Helper methods

    private async Task<Guid?> GetStudentIdFromUserId(string userId)
    {
        var student = await _unitOfWork.Students.GetAll()
            .FirstOrDefaultAsync(s => s.UserId == userId);
        return student?.StudentId;
    }

    private async Task<bool> HasBookingWithHousingUnit(Guid studentId, Guid housingUnitId)
    {
        // Check if student has a booking with this housing unit (any status)
        var hasBooking = await _unitOfWork.Bookings.GetAll()
            .AnyAsync(b => b.StudentId == studentId && 
                         b.HousingUnitId == housingUnitId);
        
        return hasBooking;
    }

    private async Task<bool> HasExistingReview(Guid studentId, Guid housingUnitId)
    {
        // Check if student already has a review for this housing unit
        var hasReview = await _unitOfWork.Reviews.GetAll()
            .AnyAsync(r => r.StudentId == studentId && 
                         r.HousingUnitId == housingUnitId);
        
        return hasReview;
    }

    private async Task SendReviewNotificationToLandlord(Guid reviewId, Guid housingUnitId, int rating)
    {
        try
        {
            var housingUnit = await _unitOfWork.HousingUnits.GetAsync(housingUnitId);
            if (housingUnit == null) return;

            var landlord = await _unitOfWork.LandLords.GetAsync(housingUnit.LandLordId);
            if (landlord == null || string.IsNullOrEmpty(landlord.UserId)) return;

            var ratingText = rating switch
            {
                5 => "5 stars - Excellent",
                4 => "4 stars - Very Good",
                3 => "3 stars - Good",
                2 => "2 stars - Fair",
                1 => "1 star - Poor",
                _ => $"{rating} stars"
            };

            await _notificationService.SendRealTimeNotificationAsync(
                landlord.UserId,
                $"Your property has received a new review ({ratingText}). Review ID: {reviewId}",
                NotificationTypes.NewReviewReceived
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending review notification to landlord");
        }
    }
}
