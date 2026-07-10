using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Business.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace Business.Services;

public class PaymobService : IPaymobService
{
    private readonly PaymobSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymobService> _logger;
    private readonly IConfiguration _config;

    public PaymobService(IOptions<PaymobSettings> settings, HttpClient httpClient, ILogger<PaymobService> logger, IConfiguration config)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
        _logger = logger;
        _config = config;
        // Do NOT set BaseAddress here — we use absolute URIs per request
    }

    // Legacy stubs — kept for interface compatibility
    public Task<string> AuthenticateAsync() => Task.FromResult(string.Empty);
    public Task<PaymobPaymentResponse> CreateOrderAsync(decimal amount, string currency = "EGP")
        => Task.FromResult(new PaymobPaymentResponse { Success = false, Message = "Use InitiatePaymentAsync" });
    public Task<PaymobPaymentResponse> CreatePaymentKeyAsync(string orderId, decimal amount, PaymobPaymentRequest request)
        => Task.FromResult(new PaymobPaymentResponse { Success = false, Message = "Use InitiatePaymentAsync" });

    /// <summary>
    /// Initiates payment using Paymob Intention API (v1).
    /// Docs: https://developers.paymob.com/egypt/docs/intention-api
    /// </summary>
    public async Task<PaymobPaymentResponse> InitiatePaymentAsync(PaymobPaymentRequest request)
    {
        try
        {
            _logger?.LogInformation("Paymob: _settings.ApiKey from IOptions (length: {Length}, starts with: {Prefix})", 
                _settings.ApiKey?.Length ?? 0, 
                _settings.ApiKey?.Length > 10 ? _settings.ApiKey.Substring(0, 10) + "..." : "null/empty");
            
            var effectiveApiKey = ResolveApiKey();
            _logger?.LogInformation("Paymob: Resolved API key (length: {Length}, starts with: {Prefix})", 
                effectiveApiKey?.Length ?? 0, 
                effectiveApiKey?.Length > 10 ? effectiveApiKey.Substring(0, 10) + "..." : "null/empty");
            
            if (string.IsNullOrWhiteSpace(effectiveApiKey))
            {
                _logger?.LogError("Paymob Secret Key is not configured. Resolved API key is null or empty.");
                return new PaymobPaymentResponse { Success = false, Message = "Paymob Secret Key is not configured" };
            }
            _logger?.LogInformation("Paymob: Using API key starting with: {Prefix}", effectiveApiKey.Substring(0, Math.Min(8, effectiveApiKey.Length)) + "...");

            if (string.IsNullOrWhiteSpace(_settings.CardIntegrationId) ||
                !int.TryParse(_settings.CardIntegrationId, out var integrationId))
                return new PaymobPaymentResponse { Success = false, Message = "Paymob CardIntegrationId is not configured or invalid" };

            var firstName = request.CustomerName?.Split(' ').FirstOrDefault() ?? "Customer";
            var lastName = request.CustomerName?.Contains(' ') == true
                ? string.Join(" ", request.CustomerName.Split(' ').Skip(1))
                : "NA";

            var intentionBody = new
            {
                amount = (long)(request.Amount * 100),
                currency = request.Currency ?? "EGP",
                payment_methods = new[] { integrationId },
                items = new[]
                {
                    new
                    {
                        name = request.Description ?? "Booking Payment",
                        amount = (long)(request.Amount * 100),
                        description = request.Description ?? "Housing Booking Payment",
                        quantity = 1
                    }
                },
                billing_data = new
                {
                    first_name = firstName,
                    last_name = lastName,
                    email = request.CustomerEmail,
                    phone_number = request.CustomerPhone,
                    apartment = "NA", floor = "NA", street = "NA", building = "NA",
                    shipping_method = "NA", postal_code = "NA",
                    city = "Cairo", country = "EG", state = "Cairo"
                },
                customer = new
                {
                    first_name = firstName,
                    last_name = lastName,
                    email = request.CustomerEmail,
                    phone_number = request.CustomerPhone
                },
                extras = request.Metadata ?? new Dictionary<string, string>()
            };

            var json = JsonSerializer.Serialize(intentionBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Use per-request message to avoid DefaultRequestHeaders concurrency issues
            // Build intention URL on the authority (host) because the intention API sits at the root
            // e.g. https://accept.paymob.com/v1/intention/ while some other endpoints use /api/*
            // Resolve current settings from configuration to handle runtime overrides
            var currentBaseUrl = ResolveSetting("BaseUrl") ?? _settings.BaseUrl;
            var baseAuthority = new Uri(currentBaseUrl).GetLeftPart(UriPartial.Authority);
            var intentionUrl = $"{baseAuthority.TrimEnd('/')}/v1/intention/";
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, intentionUrl);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Token", effectiveApiKey);
            requestMessage.Content = content;

            // Log minimal diagnostic information without revealing the secret key
            var mask = effectiveApiKey.Length > 8 ? effectiveApiKey.Substring(0, 8) + "..." : "***";
            _logger?.LogInformation("Paymob: POST {Url}. Authorization header: {Auth}", intentionUrl, $"Token {mask}");

            var response = await _httpClient.SendAsync(requestMessage);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new PaymobPaymentResponse
                {
                    Success = false,
                    Message = $"Paymob intention failed ({(int)response.StatusCode}): {responseContent}",
                    RawResponse = responseContent
                };

            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var clientSecret = result.TryGetProperty("client_secret", out var cs) ? cs.GetString() : null;
            var intentionId = result.TryGetProperty("id", out var id) ? id.GetString() : null;

            if (string.IsNullOrEmpty(clientSecret))
                return new PaymobPaymentResponse
                {
                    Success = false,
                    Message = $"Paymob returned no client_secret. Response: {responseContent}",
                    RawResponse = responseContent
                };

            // Hosted checkout URL using Public Key + client_secret
            var publicKey = ResolveSetting("PublicKey") ?? _settings.PublicKey;
            if (string.IsNullOrEmpty(publicKey) && !string.IsNullOrEmpty(effectiveApiKey) && effectiveApiKey.Contains("_sk_"))
                publicKey = effectiveApiKey.Replace("_sk_", "_pk_");
            var paymentUrl = $"https://accept.paymob.com/unifiedcheckout/?publicKey={publicKey}&clientSecret={clientSecret}";

            return new PaymobPaymentResponse
            {
                Success = true,
                OrderId = intentionId,
                IntentionId = intentionId,
                PaymentToken = clientSecret,
                PaymentUrl = paymentUrl,
                RawResponse = responseContent
            };
        }
        catch (Exception ex)
        {
            return new PaymobPaymentResponse { Success = false, Message = $"Payment initiation failed: {ex.Message}" };
        }
    }

    public async Task<bool> ValidateCallbackHmacAsync(PaymobCallbackResponse callback)
    {
        if (callback?.Obj?.TransactionData?.Hmac == null) return false;

        var td = callback.Obj.TransactionData;
        var concat = $"{callback.Obj.Id}{callback.Obj.OrderId}{td.Amount}{td.Currency}{td.Success}{td.IntegrationId}";

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_settings.HmacSecret));
        var computed = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(concat))).Replace("-", "").ToLower();
        return computed == td.Hmac?.ToLower();
    }

    public async Task<PaymobPaymentResponse> ProcessPaymentCallbackAsync(PaymobCallbackResponse callback)
    {
        try
        {
            if (!await ValidateCallbackHmacAsync(callback))
                return new PaymobPaymentResponse { Success = false, Message = "Invalid HMAC signature" };

            var isSuccess = callback.Obj?.TransactionData?.Success == "true";
            return new PaymobPaymentResponse
            {
                Success = isSuccess,
                OrderId = callback.Obj?.OrderId,
                PaymentToken = callback.Obj?.TransactionData?.Id,
                Message = isSuccess ? "Payment completed" : "Payment failed",
                RawResponse = JsonSerializer.Serialize(callback)
            };
        }
        catch (Exception ex)
        {
            return new PaymobPaymentResponse { Success = false, Message = $"Callback error: {ex.Message}" };
        }
    }

    public async Task<bool> RefundTransactionAsync(string transactionId, decimal amount)
    {
        var refundRequest = new { transaction_id = transactionId, amount_cents = (long)(amount * 100) };
        var content = new StringContent(JsonSerializer.Serialize(refundRequest), Encoding.UTF8, "application/json");
        
        var currentBase = ResolveSetting("BaseUrl") ?? _settings.BaseUrl;
        var baseAuthority = new Uri(currentBase).GetLeftPart(UriPartial.Authority);
        // Ensure API base contains /api segment as required by Paymob for certain endpoints
        string apiBase;
        if (currentBase.TrimEnd('/').EndsWith("/api", StringComparison.OrdinalIgnoreCase) || currentBase.IndexOf("/api", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            apiBase = currentBase.TrimEnd('/');
        }
        else
        {
            apiBase = baseAuthority.TrimEnd('/') + "/api";
        }

        var refundUrl = $"{apiBase}/acceptance/void_refund/refund";
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, refundUrl);
        var effectiveApiKey = ResolveApiKey();
        requestMessage.Headers.TryAddWithoutValidation("Authorization", $"Token {effectiveApiKey}");
        requestMessage.Content = content;
        
        var response = await _httpClient.SendAsync(requestMessage);
        return response.IsSuccessStatusCode;
    }

    private string? ResolveApiKey()
    {
        // Read fresh values from configuration each time to avoid stale snapshots
        try
        {
            var section = _config?.GetSection(PaymobSettings.SectionName);
            var apiKey = section?["ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                _logger?.LogInformation("Paymob: API key found in configuration section 'ApiKey' (attempting decode if needed).");
                // If it looks like base64, try to decode and extract real secret
                bool looksBase64 = apiKey.Length % 4 == 0 && System.Text.RegularExpressions.Regex.IsMatch(apiKey, "^[A-Za-z0-9+/=\\r\\n]+$");
                if (looksBase64)
                {
                    try
                    {
                        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(apiKey));
                        if (!string.IsNullOrWhiteSpace(decoded) && decoded.Contains("egy_sk_"))
                        {
                            _logger?.LogInformation("Paymob: decoded ApiKey from base64 and found secret prefix");
                            return decoded;
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }

                // If it already looks like the secret, return as-is; otherwise return the provided value
                if (apiKey.StartsWith("egy_sk_") || apiKey.StartsWith("sk_"))
                    return apiKey;

                return apiKey;
            }

            var secretFromConfig = section?["SecretKey"] ?? _config?["Paymob:SecretKey"];
            if (!string.IsNullOrWhiteSpace(secretFromConfig))
            {
                _logger?.LogInformation("Paymob: API key resolved from configuration section 'SecretKey'.");
                return secretFromConfig;
            }

            // Environment variables fallback
            var env = Environment.GetEnvironmentVariable("PAYMOB_SECRET")
                   ?? Environment.GetEnvironmentVariable("PAYMOB_APIKEY")
                   ?? Environment.GetEnvironmentVariable("PAYMOB_API_KEY")
                   ?? _config?["PAYMOB_SECRET"];
            if (!string.IsNullOrWhiteSpace(env))
            {
                _logger?.LogInformation("Paymob: API key resolved from environment variable.");
                return env;
            }

            // Fallback to settings object (injected via IOptions) - loaded at startup
            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger?.LogInformation("Paymob: API key resolved from IOptions settings object.");
                // If it's a base64 encoded value, try to decode it
                if (!_settings.ApiKey.StartsWith("egy_sk_"))
                {
                    try
                    {
                        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(_settings.ApiKey));
                        if (!string.IsNullOrWhiteSpace(decoded) && decoded.StartsWith("egy_sk_"))
                        {
                            _logger?.LogInformation("Paymob: API key was base64 decoded.");
                            return decoded;
                        }
                    }
                    catch
                    {
                        // Not base64, return as-is
                    }
                }
                return _settings.ApiKey;
            }
            
            _logger?.LogError("Paymob: Could not resolve API key from any source.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Paymob: Error resolving API key.");
        }

        return null;
    }

    private string? ResolveSetting(string key)
    {
        try
        {
            var section = _config?.GetSection(PaymobSettings.SectionName);
            var val = section?[key];
            if (!string.IsNullOrWhiteSpace(val))
                return val;
            return _config?["Paymob:" + key];
        }
        catch
        {
            return null;
        }
    }
}
