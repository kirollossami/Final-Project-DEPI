namespace Business.DTOs.Responses;

public class PaymobPaymentResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? OrderId { get; set; }
    public string? IntentionId { get; set; }
    public string? PaymentToken { get; set; }
    public string? PaymentUrl { get; set; }
    public string? RawResponse { get; set; }
}

/// <summary>
/// Parsed response from POST /api/auth/tokens.
/// The token field is a short-lived JWT (≈1 hour) used as Bearer on
/// all /api/acceptance/* endpoints.
/// </summary>
public class PaymobAuthResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Message { get; set; }
}

public class PaymobTransactionVerificationResponse
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? OrderId { get; set; }
    public string? MerchantOrderId { get; set; }   // intention ID (pi_test_xxx)
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public bool IsSuccess { get; set; }
    public bool IsCaptured { get; set; }
    public bool IsVoided { get; set; }
    public bool IsRefunded { get; set; }
    public string? Data { get; set; }
    public string? RawResponse { get; set; }
}

/// <summary>
/// Detailed transaction details from Paymob API
/// GET https://accept.paymob.com/api/acceptance/transactions/{transactionId}
/// </summary>
public class PaymobTransactionDetails
{
    public PaymobTransactionOrder? Order { get; set; }
    public bool Success { get; set; }
    public long AmountCents { get; set; }
    public string? Currency { get; set; }
    public bool IsAuth { get; set; }
    public bool IsCapture { get; set; }
    public bool IsRefunded { get; set; }
    public bool IsVoided { get; set; }
    public bool Is3DSecure { get; set; }
    public string? IntegrationId { get; set; }
    public PaymobTransactionSource? SourceData { get; set; }
    public string? ErrorOccured { get; set; }
    public string? DataMessage { get; set; }
    public string? TxnResponseCode { get; set; }
}

public class PaymobTransactionOrder
{
    public long Id { get; set; }
    public string? MerchantOrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long AmountCents { get; set; }
    public string? Currency { get; set; }
}

public class PaymobTransactionSource
{
    public string? Type { get; set; }
    public string? Pan { get; set; }
    public string? SubType { get; set; }
}
