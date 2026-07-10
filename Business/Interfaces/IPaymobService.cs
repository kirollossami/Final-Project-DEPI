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

    /// <summary>
    /// Authenticates with Paymob and returns a short-lived Bearer token.
    /// POST https://accept.paymob.com/api/auth/tokens
    /// This token is required for /api/acceptance/* endpoints (e.g. transaction retrieval).
    /// The Intention API (/v1/intention/) uses Token {api_key} instead.
    /// </summary>
    Task<PaymobAuthResponse> GetBearerTokenAsync();

    /// <summary>
    /// Retrieves full transaction details from Paymob using Bearer token auth.
    /// GET https://accept.paymob.com/api/acceptance/transactions/{id}
    /// </summary>
    Task<PaymobTransactionDetails?> GetTransactionDetailsAsync(string transactionId);

    /// <summary>
    /// Retrieves full transaction details as JsonElement for backward compatibility.
    /// GET https://accept.paymob.com/api/acceptance/transactions/{id}
    /// </summary>
    Task<System.Text.Json.JsonElement?> GetTransactionDetailsJsonAsync(string transactionId);

    Task<System.Text.Json.JsonElement?> GetIntentionDetailsAsync(string intentionId);
}
