using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse?> GetPaymentByIdAsync(Guid paymentId);
    Task<PaymentIndexedResponse> GetPaymentsAsync(PaymentFilterRequest filter);
    Task<PaymentResponse?> CreatePaymentAsync(PaymentCreateRequest request);
    Task<PaymentResponse?> UpdatePaymentAsync(PaymentUpdateRequest request);
}
