using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Business.Services;

/// <summary>
/// Service for managing payment history and audit logs
/// </summary>
public class PaymentHistoryService : IPaymentHistoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentHistoryService> _logger;

    public PaymentHistoryService(
        IUnitOfWork unitOfWork,
        ILogger<PaymentHistoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> RecordPaymentEventAsync(
        Guid paymentId,
        Guid? bookingId,
        Guid? escrowId,
        string userId,
        string eventType,
        string description,
        decimal amount,
        string previousStatus,
        string newStatus,
        string? actorUserId = null,
        string? actorRole = null,
        string? ipAddress = null,
        Dictionary<string, object>? metadata = null)
    {
        try
        {
            var paymentHistory = new PaymentHistory
            {
                HistoryId = Guid.NewGuid(),
                PaymentId = paymentId,
                BookingId = bookingId,
                EscrowTransactionId = escrowId,
                UserId = userId,
                EventType = eventType,
                Description = description,
                Amount = amount,
                Currency = "EGP",
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                ActorUserId = actorUserId,
                ActorRole = actorRole,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null
            };

            await _unitOfWork.PaymentHistories.Insert(paymentHistory);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                $"Payment event recorded: PaymentId={paymentId}, EventType={eventType}, UserId={userId}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording payment event");
            return false;
        }
    }

    public async Task<IEnumerable<PaymentHistoryResponse>> GetUserPaymentHistoryAsync(string userId)
    {
        try
        {
            var history = await _unitOfWork.PaymentHistories.GetPaymentHistoryByUserAsync(userId);

            return history.Select(h => MapToResponse(h)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving payment history for user {userId}");
            return Enumerable.Empty<PaymentHistoryResponse>();
        }
    }

    public async Task<IEnumerable<PaymentHistoryResponse>> GetBookingPaymentHistoryAsync(Guid bookingId)
    {
        try
        {
            var history = await _unitOfWork.PaymentHistories.GetPaymentHistoryByBookingAsync(bookingId);

            return history.Select(h => MapToResponse(h)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving payment history for booking {bookingId}");
            return Enumerable.Empty<PaymentHistoryResponse>();
        }
    }

    public async Task<IEnumerable<PaymentHistoryResponse>> GetPaymentTransactionHistoryAsync(Guid paymentId)
    {
        try
        {
            var history = await _unitOfWork.PaymentHistories.GetPaymentHistoryByPaymentAsync(paymentId);

            return history.Select(h => MapToResponse(h)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving payment history for payment {paymentId}");
            return Enumerable.Empty<PaymentHistoryResponse>();
        }
    }

    public async Task<IEnumerable<PaymentHistoryResponse>> GetPaymentHistoryByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var history = await _unitOfWork.PaymentHistories.GetPaymentHistoryByDateRangeAsync(startDate, endDate);

            return history.Select(h => MapToResponse(h)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment history by date range");
            return Enumerable.Empty<PaymentHistoryResponse>();
        }
    }

    private PaymentHistoryResponse MapToResponse(PaymentHistory history)
    {
        Dictionary<string, object>? metadata = null;

        if (!string.IsNullOrEmpty(history.MetadataJson))
        {
            try
            {
                metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(history.MetadataJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deserializing payment history metadata");
            }
        }

        return new PaymentHistoryResponse
        {
            HistoryId = history.HistoryId,
            PaymentId = history.PaymentId,
            BookingId = history.BookingId,
            EscrowTransactionId = history.EscrowTransactionId,
            UserId = history.UserId,
            EventType = history.EventType,
            Description = history.Description,
            Amount = history.Amount,
            Currency = history.Currency,
            PreviousStatus = history.PreviousStatus,
            NewStatus = history.NewStatus,
            ActorUserId = history.ActorUserId,
            ActorRole = history.ActorRole,
            CreatedAt = history.CreatedAt,
            Metadata = metadata
        };
    }
}
