using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IBookingPaymentService
{
    Task<BookingPaymentResponse> InitiateBookingPaymentAsync(BookingPaymentRequest request);
    Task<BookingPaymentResponse> ProcessPaymentCallbackAsync(
        string orderId, 
        string transactionId, 
        string clientSecret, 
        bool isSuccess, 
        string? bookingId = null, 
        string? paymentId = null,
        string? merchantOrderId = null);
    Task<BookingPaymentResponse> CompleteBookingWorkflowAsync(Guid paymentId);
    Task<int> SyncPendingPaymentsAsync();
}
