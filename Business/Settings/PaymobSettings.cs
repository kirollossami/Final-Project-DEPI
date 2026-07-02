namespace Business.Settings;

public class PaymobSettings
{
    public const string SectionName = "Paymob";
    
    public string ApiKey { get; set; } = string.Empty;
    public string IntegrationId { get; set; } = string.Empty;
    public string WalletIntegrationId { get; set; } = string.Empty;
    public string CardIntegrationId { get; set; } = string.Empty;
    public string IFrameId { get; set; } = string.Empty;
    public string HmacSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://accept.paymob.com/api";
    public bool IsSandbox { get; set; } = true;
}
