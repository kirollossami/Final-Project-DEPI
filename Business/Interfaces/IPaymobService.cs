using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IPaymobService
{
    Task<string> AuthenticateAsync();
    Task<PaymobPaymentResponse> CreateOrderAsync(decimal amount, string currency = "EGP");
    Task<PaymobPaymentResponse> CreatePaymentKeyAsync(
        string orderId,
        decimal amount,
        PaymobPaymentRequest request);
    Task<PaymobPaymentResponse> InitiatePaymentAsync(PaymobPaymentRequest request);
    Task<bool> ValidateCallbackHmacAsync(PaymobCallbackResponse callback);
    Task<PaymobPaymentResponse> ProcessPaymentCallbackAsync(PaymobCallbackResponse callback);
    Task<bool> RefundTransactionAsync(string transactionId, decimal amount);
}
