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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymobService> _logger;
    private readonly IConfiguration _config;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // ── Bearer token cache ────────────────────────────────────────────────────
    // Paymob Bearer tokens expire after ~1 hour. We cache and reuse them to
    // avoid one extra HTTP call per transaction lookup. The cache is invalidated
    // 5 minutes before the real expiry to guard against clock skew.
    private string? _cachedBearerToken;
    private DateTime _bearerTokenExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _bearerTokenLock = new(1, 1);
    private static readonly TimeSpan BearerTokenTtl        = TimeSpan.FromMinutes(55); // tokens live ~60 min
    private static readonly TimeSpan BearerTokenSafetyLead = TimeSpan.FromMinutes(5);  // refresh 5 min early

    public PaymobService(
        IOptions<PaymobSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<PaymobService> logger,
        IConfiguration config,
        IHttpContextAccessor httpContextAccessor)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
        _httpContextAccessor = httpContextAccessor;

        // Log LegacyApiKey configuration status
        var legacyKey = ResolveSetting("LegacyApiKey") ?? _settings.LegacyApiKey;
        if (!string.IsNullOrWhiteSpace(legacyKey))
        {
            _logger.LogInformation("Paymob: LegacyApiKey configured: ✅ YES (length: {Length})", legacyKey.Length);
        }
        else
        {
            _logger.LogWarning("Paymob: LegacyApiKey configured: ❌ NO - transaction lookups will fail");
        }
    }

    // ── Legacy stubs — kept for interface compatibility ───────────────────────
    public Task<string> AuthenticateAsync() => Task.FromResult(string.Empty);
    public Task<PaymobPaymentResponse> CreateOrderAsync(decimal amount, string currency = "EGP")
        => Task.FromResult(new PaymobPaymentResponse { Success = false, Message = "Use InitiatePaymentAsync" });
    public Task<PaymobPaymentResponse> CreatePaymentKeyAsync(string orderId, decimal amount, PaymobPaymentRequest request)
        => Task.FromResult(new PaymobPaymentResponse { Success = false, Message = "Use InitiatePaymentAsync" });

    // ─────────────────────────────────────────────────────────────────────────
    // GetBearerTokenAsync
    // POST https://accept.paymob.com/api/auth/tokens
    // Body: { "api_key": "<LEGACY_API_KEY>" }   ← NOT the secret key (egy_sk_...)
    //
    // Paymob has two separate key types:
    //   Secret Key (egy_sk_...) → used ONLY for /v1/intention/ (Token header)
    //   Legacy API Key          → used ONLY for /api/auth/tokens to get Bearer token
    //
    // The Bearer token returned is then used for all /api/acceptance/* endpoints.
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<PaymobAuthResponse> GetBearerTokenAsync()
    {
        // Fast path — return cached token if still valid
        if (!string.IsNullOrEmpty(_cachedBearerToken)
            && DateTime.UtcNow < _bearerTokenExpiresAt - BearerTokenSafetyLead)
        {
            _logger.LogDebug("Paymob: Using cached Bearer token (expires {Expiry:HH:mm:ss} UTC)", _bearerTokenExpiresAt);
            return new PaymobAuthResponse { Success = true, Token = _cachedBearerToken };
        }

        await _bearerTokenLock.WaitAsync();
        try
        {
            // Re-check inside the lock
            if (!string.IsNullOrEmpty(_cachedBearerToken)
                && DateTime.UtcNow < _bearerTokenExpiresAt - BearerTokenSafetyLead)
                return new PaymobAuthResponse { Success = true, Token = _cachedBearerToken };

            // Resolve the Legacy API Key — separate from the Secret Key (egy_sk_...)
            // Priority: Paymob:LegacyApiKey → env PAYMOB_LEGACY_KEY → fallback to ApiKey (may fail)
            var legacyKey = ResolveSetting("LegacyApiKey")
                         ?? _settings.LegacyApiKey
                         ?? Environment.GetEnvironmentVariable("PAYMOB_LEGACY_KEY");

            if (string.IsNullOrWhiteSpace(legacyKey))
            {
                // Fallback: some Paymob accounts accept the secret key here
                legacyKey = ResolveApiKey();
                _logger.LogWarning(
                    "Paymob: LegacyApiKey not configured in Paymob:LegacyApiKey. " +
                    "Falling back to ApiKey — this will return 403 if your account requires a separate legacy key. " +
                    "Go to Paymob Dashboard → Settings → Account Info to get your Legacy API Key.");
            }

            if (string.IsNullOrWhiteSpace(legacyKey))
            {
                _logger.LogError("Paymob: GetBearerTokenAsync — no API key configured at all.");
                return new PaymobAuthResponse { Success = false, Message = "Paymob API key not configured" };
            }

            var masked  = legacyKey.Length > 8 ? legacyKey[..8] + "..." : "***";
            var baseUrl  = ResolveSetting("BaseUrl") ?? _settings.BaseUrl;
            var authority = new Uri(baseUrl).GetLeftPart(UriPartial.Authority);
            var authUrl   = $"{authority.TrimEnd('/')}/api/auth/tokens";

            _logger.LogInformation(
                "Paymob: POST {AuthUrl} — fetching Bearer token. Key prefix: {Masked}", authUrl, masked);

            var body    = JsonSerializer.Serialize(new { api_key = legacyKey });
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            HttpResponseMessage resp;
            string respBody;
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, authUrl) { Content = content };
                resp     = await httpClient.SendAsync(req);
                respBody = await resp.Content.ReadAsStringAsync();
            }
            catch (Exception httpEx)
            {
                _logger.LogError(httpEx, "Paymob: HTTP error calling {AuthUrl}", authUrl);
                return new PaymobAuthResponse { Success = false, Message = $"HTTP error: {httpEx.Message}" };
            }

            _logger.LogInformation(
                "Paymob: Auth response {Status} from {AuthUrl}", (int)resp.StatusCode, authUrl);

            if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogError(
                    "Paymob: 403 Forbidden on /api/auth/tokens. " +
                    "This means the key sent is NOT a valid Legacy API Key. " +
                    "Steps to fix: " +
                    "(1) Log in to accept.paymob.com, " +
                    "(2) Go to Settings → Account Info, " +
                    "(3) Copy the 'API Key' field (NOT the secret key), " +
                    "(4) Set it in appsettings as Paymob:LegacyApiKey. " +
                    "Response body: {Body}", respBody);
                return new PaymobAuthResponse
                {
                    Success = false,
                    Message = "403 Forbidden — wrong key type for /api/auth/tokens. " +
                              "Set Paymob:LegacyApiKey in appsettings (Dashboard → Settings → Account Info → API Key)"
                };
            }

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Paymob: Auth failed ({Status}). Body: {Body}", (int)resp.StatusCode, respBody);
                return new PaymobAuthResponse
                {
                    Success = false,
                    Message = $"Paymob auth failed ({(int)resp.StatusCode}): {respBody}"
                };
            }

            JsonElement json;
            try { json = JsonSerializer.Deserialize<JsonElement>(respBody); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paymob: Cannot deserialize auth response: {Body}", respBody);
                return new PaymobAuthResponse { Success = false, Message = "Invalid auth response" };
            }

            if (!json.TryGetProperty("token", out var tokenProp) || string.IsNullOrWhiteSpace(tokenProp.GetString()))
            {
                _logger.LogError("Paymob: Auth response has no 'token'. Body: {Body}", respBody);
                return new PaymobAuthResponse { Success = false, Message = "Paymob auth response missing token" };
            }

            var token = tokenProp.GetString()!;
            _cachedBearerToken    = token;
            _bearerTokenExpiresAt = DateTime.UtcNow.Add(BearerTokenTtl);

            _logger.LogInformation(
                "Paymob: Bearer token obtained and cached until ~{Expiry:HH:mm:ss} UTC",
                _bearerTokenExpiresAt);

            return new PaymobAuthResponse { Success = true, Token = token };
        }
        finally
        {
            _bearerTokenLock.Release();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetTransactionDetailsAsync
    // GET https://accept.paymob.com/api/acceptance/transactions/{transactionId}
    //
    // Auth strategy (tried in order):
    //   1. Bearer {LegacyApiKey} - if configured
    //   2. Token {SecretKey} - fallback for newer Paymob accounts
    //
    // Key fields in the response used by callback reconciliation:
    //   .order.id               → numeric Paymob order ID  (e.g. 563150958)
    //   .order.merchant_order_id → our intention ID stored locally (pi_test_xxx)
    //   .success                → bool
    //   .amount_cents           → amount × 100
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<PaymobTransactionDetails?> GetTransactionDetailsAsync(string transactionId)
    {
        _logger.LogInformation(
            "Paymob: GetTransactionDetailsAsync START | TransactionId={TxnId}", transactionId);

        var baseUrl   = ResolveSetting("BaseUrl") ?? _settings.BaseUrl;
        var authority = new Uri(baseUrl).GetLeftPart(UriPartial.Authority);
        var txnUrl    = $"{authority.TrimEnd('/')}/api/acceptance/transactions/{transactionId}";
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        // ── Attempt 1: Bearer {LegacyApiKey} ─────────────────────────────────
        var legacyKey = ResolveSetting("LegacyApiKey")
                      ?? _settings.LegacyApiKey
                      ?? Environment.GetEnvironmentVariable("PAYMOB_LEGACY_KEY");

        if (!string.IsNullOrWhiteSpace(legacyKey))
        {
            var maskedKey = legacyKey.Length > 12 ? legacyKey.Substring(0, 12) + "..." : "***";
            _logger.LogInformation(
                "Paymob: GET {Url} - Attempt 1: Bearer token (length: {Length})",
                txnUrl, legacyKey.Length);

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, txnUrl);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", legacyKey);
                
                var resp = await httpClient.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Paymob: Response status: 200 OK (Bearer auth)");
                    return ParseTransactionDetails(body, transactionId);
                }

                _logger.LogWarning(
                    "Paymob: Bearer auth failed ({Status}). Body: {Body}",
                    (int)resp.StatusCode, body);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Paymob: Bearer auth request timed out");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Paymob: Bearer auth HTTP error");
            }
        }
        else
        {
            _logger.LogInformation("Paymob: LegacyApiKey not configured, skipping Bearer auth");
        }

        // ── Attempt 2: Token {SecretKey} ─────────────────────────────────────
        var secretKey = ResolveApiKey();
        if (!string.IsNullOrWhiteSpace(secretKey))
        {
            var maskedKey = secretKey.Length > 12 ? secretKey.Substring(0, 12) + "..." : "***";
            _logger.LogInformation(
                "Paymob: GET {Url} - Attempt 2: Token auth (length: {Length})",
                txnUrl, secretKey.Length);

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, txnUrl);
                req.Headers.Authorization = new AuthenticationHeaderValue("Token", secretKey);
                
                var resp = await httpClient.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Paymob: Response status: 200 OK (Token auth)");
                    return ParseTransactionDetails(body, transactionId);
                }

                _logger.LogError(
                    "Paymob: Token auth failed ({Status}). Body: {Body}",
                    (int)resp.StatusCode, body);
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Paymob: Token auth request timed out");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paymob: Token auth HTTP error");
            }
        }

        _logger.LogError("Paymob: All auth strategies failed for transaction {TxnId}", transactionId);
        return null;
    }

    private PaymobTransactionDetails? ParseTransactionDetails(string body, string transactionId)
    {
        try
        {
            var details = JsonSerializer.Deserialize<PaymobTransactionDetails>(body);
            if (details == null)
            {
                _logger.LogError("Paymob: Failed to deserialize transaction response");
                return null;
            }

            _logger.LogInformation(
                "Paymob: Transaction details retrieved successfully");
            _logger.LogInformation(
                "Paymob: Transaction {TxnId} - OrderId={OrderId}, Amount={Amount}, Success={Success}",
                transactionId, details.Order?.Id ?? 0, details.AmountCents, details.Success);

            return details;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paymob: Error parsing transaction response");
            return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetTransactionDetailsJsonAsync
    // Returns JsonElement for backward compatibility with existing code
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<JsonElement?> GetTransactionDetailsJsonAsync(string transactionId)
    {
        var details = await GetTransactionDetailsAsync(transactionId);
        if (details == null) return null;

        // Convert back to JsonElement for backward compatibility
        var json = JsonSerializer.SerializeToElement(details);
        return json;
    }

    // Executes a GET and returns (JsonElement?, statusCode, responseBody)
    private async Task<(JsonElement?, System.Net.HttpStatusCode, string)> DoGetAsync(
        HttpClient client, string url, AuthenticationHeaderValue auth)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = auth;
            var resp = await client.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (null, resp.StatusCode, body);

            var json = JsonSerializer.Deserialize<JsonElement>(body);
            return (json, resp.StatusCode, body);
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("Paymob: Request to {Url} timed out", url);
            return (null, System.Net.HttpStatusCode.RequestTimeout, "timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paymob: HTTP error on GET {Url}", url);
            return (null, System.Net.HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private void LogReconciliationFields(JsonElement json, string transactionId)
    {
        string? numericOrderId  = null;
        string? merchantOrderId = null;
        bool    success         = false;
        long    amountCents     = 0;

        if (json.TryGetProperty("order", out var orderEl))
        {
            if (orderEl.TryGetProperty("id", out var oid))
                numericOrderId = oid.ValueKind == JsonValueKind.Number
                    ? oid.GetInt64().ToString() : oid.GetString();
            if (orderEl.TryGetProperty("merchant_order_id", out var moid)
                && moid.ValueKind != JsonValueKind.Null)
                merchantOrderId = moid.GetString();
        }
        if (json.TryGetProperty("success",      out var sp)) success     = sp.ValueKind == JsonValueKind.True;
        if (json.TryGetProperty("amount_cents", out var ac)) amountCents = ac.GetInt64();

        _logger.LogInformation(
            "Paymob transaction fields | TxnId={TxnId} Success={Success} " +
            "AmountEGP={Amt} NumericOrderId={OrderId} MerchantOrderId={MerchantId}",
            transactionId, success, amountCents / 100m, numericOrderId, merchantOrderId);
    }

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

            // Resolve dynamic redirection URL back to backend's callback redirect endpoint
            string? redirectionUrl = null;
            try
            {
                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext?.Request != null)
                {
                    var req = httpContext.Request;
                    redirectionUrl = $"{req.Scheme}://{req.Host}/api/BookingPayment/callback";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not resolve dynamic redirection URL from HTTP Context");
            }

            if (string.IsNullOrEmpty(redirectionUrl))
            {
                redirectionUrl = "https://unistay.tryasp.net/api/BookingPayment/callback";
            }

            _logger?.LogInformation("Paymob: setting redirection_url to: {RedirectionUrl}", redirectionUrl);

            var intentionBody = new
            {
                amount = (long)(request.Amount * 100),
                currency = request.Currency ?? "EGP",
                payment_methods = new[] { integrationId },
                redirection_url = redirectionUrl,
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
            
            var httpClient = _httpClientFactory.CreateClient();
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, intentionUrl);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Token", effectiveApiKey);
            requestMessage.Content = content;

            // Log minimal diagnostic information without revealing the secret key
            var mask = effectiveApiKey.Length > 8 ? effectiveApiKey.Substring(0, 8) + "..." : "***";
            _logger?.LogInformation("Paymob: POST {Url}. Authorization header: {Auth}", intentionUrl, $"Token {mask}");

            var response = await httpClient.SendAsync(requestMessage);
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
        // Refund also needs Bearer token auth
        var authResult = await GetBearerTokenAsync();
        if (!authResult.Success || string.IsNullOrWhiteSpace(authResult.Token))
        {
            _logger.LogError("Paymob: Cannot refund — Bearer token unavailable: {Msg}", authResult.Message);
            return false;
        }

        var baseUrl   = ResolveSetting("BaseUrl") ?? _settings.BaseUrl;
        var authority = new Uri(baseUrl).GetLeftPart(UriPartial.Authority);
        var refundUrl = $"{authority.TrimEnd('/')}/api/acceptance/void_refund/refund";

        var body    = JsonSerializer.Serialize(new { transaction_id = transactionId, amount_cents = (long)(amount * 100) });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var httpClient = _httpClientFactory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, refundUrl) { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);

        var resp = await httpClient.SendAsync(req);
        _logger.LogInformation("Paymob: Refund response {Status} for transaction {TxnId}", (int)resp.StatusCode, transactionId);
        return resp.IsSuccessStatusCode;
    }

    /// <summary>
    /// Verifies a Paymob transaction using Bearer token auth.
    /// Returns a strongly-typed response including merchant_order_id for reconciliation.
    /// </summary>
    public async Task<PaymobTransactionVerificationResponse?> VerifyTransactionAsync(string transactionId)
    {
        var details = await GetTransactionDetailsAsync(transactionId);
        if (details == null) return null;

        try
        {
            string? numericOrderId  = details.Order?.Id.ToString();
            string? merchantOrderId = details.Order?.MerchantOrderId;

            return new PaymobTransactionVerificationResponse
            {
                Success         = true,
                TransactionId   = transactionId,
                OrderId         = numericOrderId,
                MerchantOrderId = merchantOrderId,
                Amount          = details.AmountCents / 100m,
                Currency        = details.Currency ?? "EGP",
                IsSuccess       = details.Success,
                IsCaptured      = details.IsCapture,
                IsVoided        = details.IsVoided,
                IsRefunded      = details.IsRefunded,
                RawResponse     = JsonSerializer.Serialize(details)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paymob: Error mapping transaction response for {TxnId}", transactionId);
            return null;
        }
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

    public async Task<JsonElement?> GetIntentionDetailsAsync(string intentionId)
    {
        // GET /v1/intention/{id} returns 405 on Paymob — do not call it.
        // Instead, look up transactions linked to this intention via the transaction API.
        // This method is kept for interface compatibility but delegates to transaction lookup.
        _logger?.LogInformation("Paymob: GetIntentionDetailsAsync called for {IntentionId} — note: /v1/intention GET returns 405, skipping.", intentionId);
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
