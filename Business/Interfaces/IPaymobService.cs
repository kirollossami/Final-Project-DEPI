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
    /// Requires Paymob:LegacyApiKey. Only needed for legacy accounts.
    /// Newer accounts (egy_sk_...) can use Token auth directly.
    /// </summary>
    Task<PaymobAuthResponse> GetBearerTokenAsync();

    /// <summary>
    /// Retrieves full transaction details from Paymob as a raw JsonElement.
    /// Tries Token {secret_key} first (newer accounts), then Bearer token (legacy).
    /// GET https://accept.paymob.com/api/acceptance/transactions/{id}
    /// Key fields: .order.merchant_order_id (pi_test_xxx), .order.id (numeric), .success
    /// </summary>
    Task<System.Text.Json.JsonElement?> GetTransactionDetailsAsync(string transactionId);

    /// <summary>
    /// Alias for GetTransactionDetailsAsync — kept for backward compatibility with
    /// callers that used the old method name.
    /// </summary>
    Task<System.Text.Json.JsonElement?> GetTransactionDetailsJsonAsync(string transactionId);

    Task<System.Text.Json.JsonElement?> GetIntentionDetailsAsync(string intentionId);
}
