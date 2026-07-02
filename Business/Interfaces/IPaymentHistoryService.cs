using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

/// <summary>
/// Service for managing payment history and audit logs
/// </summary>
public interface IPaymentHistoryService
{
    /// <summary>
    /// Record a payment event for audit trail
    /// </summary>
    Task<bool> RecordPaymentEventAsync(
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
        Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Get payment history for a specific user
    /// </summary>
    Task<IEnumerable<PaymentHistoryResponse>> GetUserPaymentHistoryAsync(string userId);

    /// <summary>
    /// Get payment history for a specific booking
    /// </summary>
    Task<IEnumerable<PaymentHistoryResponse>> GetBookingPaymentHistoryAsync(Guid bookingId);

    /// <summary>
    /// Get payment history for a specific payment transaction
    /// </summary>
    Task<IEnumerable<PaymentHistoryResponse>> GetPaymentTransactionHistoryAsync(Guid paymentId);

    /// <summary>
    /// Get payment history within a date range
    /// </summary>
    Task<IEnumerable<PaymentHistoryResponse>> GetPaymentHistoryByDateRangeAsync(DateTime startDate, DateTime endDate);
}
