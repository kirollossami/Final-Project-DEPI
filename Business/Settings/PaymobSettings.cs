namespace Business.Settings;

public class PaymobSettings
{
    public const string SectionName = "Paymob";
    
    public string ApiKey { get; set; } = string.Empty;       // Secret Key (egy_sk_...)
    public string PublicKey { get; set; } = string.Empty;    // Public Key (egy_pk_...)
    public string IntegrationId { get; set; } = string.Empty;
    public string WalletIntegrationId { get; set; } = string.Empty;
    public string CardIntegrationId { get; set; } = string.Empty;
    public string IFrameId { get; set; } = string.Empty;
    public string HmacSecret { get; set; } = string.Empty;
    // Base Paymob host. Intention endpoint lives at the root (https://accept.paymob.com/v1/intention/)
    // while other APIs live under the /api path. Configure this to the host (e.g. https://accept.paymob.com).
    public string BaseUrl { get; set; } = "https://accept.paymob.com";
    public bool IsSandbox { get; set; } = true;
}
