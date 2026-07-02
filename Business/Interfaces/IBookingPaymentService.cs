using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IBookingPaymentService
{
    Task<BookingPaymentResponse> InitiateBookingPaymentAsync(BookingPaymentRequest request);
    Task<BookingPaymentResponse> ProcessPaymentCallbackAsync(string orderId, string transactionId, bool isSuccess);
    Task<BookingPaymentResponse> CompleteBookingWorkflowAsync(Guid paymentId);
}
