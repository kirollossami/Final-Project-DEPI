namespace Business.DTOs.Responses;

public class PaymobCallbackResponse
{
    public string? Type { get; set; }
    public PaymobCallbackObj? Obj { get; set; }
}

public class PaymobCallbackObj
{
    public string? Id { get; set; }
    public string? OrderId { get; set; }
    public PaymobTransactionData? TransactionData { get; set; }
}

public class PaymobTransactionData
{
    public string? Id { get; set; }
    public string? Amount { get; set; }
    public string? Currency { get; set; }
    public string? Success { get; set; }
    public string? IsAuth { get; set; }
    public string? IsCapture { get; set; }
    public string? IsRefunded { get; set; }
    public string? IsVoided { get; set; }
    public string? Is3DSecure { get; set; }
    public string? IntegrationId { get; set; }
    public string? Hmac { get; set; }
    public PaymobOrderData? OrderData { get; set; }
    public PaymobCustomerData? CustomerData { get; set; }
}

public class PaymobOrderData
{
    public string? Id { get; set; }
    public string? CreatedAt { get; set; }
    public string? Amount { get; set; }
    public string? Currency { get; set; }
}

public class PaymobCustomerData
{
    public string? Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
