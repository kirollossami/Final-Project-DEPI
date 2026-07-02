using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Business.Settings;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Business.Services;

public class PaymobService : IPaymobService
{
    private readonly PaymobSettings _settings;
    private readonly HttpClient _httpClient;
    private string? _authToken;

    public PaymobService(IOptions<PaymobSettings> settings, HttpClient httpClient)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
    }

    public async Task<string> AuthenticateAsync()
    {
        var authRequest = new
        {
            api_key = _settings.ApiKey
        };

        var content = new StringContent(
            JsonSerializer.Serialize(authRequest),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync("/auth/tokens", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        _authToken = authResponse.GetProperty("token").GetString();
        return _authToken!;
    }

    public async Task<PaymobPaymentResponse> CreateOrderAsync(decimal amount, string currency = "EGP")
    {
        if (string.IsNullOrEmpty(_authToken))
        {
            await AuthenticateAsync();
        }

        var orderRequest = new
        {
            auth_token = _authToken,
            delivery_needed = false,
            amount_cents = (long)(amount * 100),
            currency = currency,
            merchant_order_id = Guid.NewGuid().ToString()
        };

        var content = new StringContent(
            JsonSerializer.Serialize(orderRequest),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync("/ecommerce/orders", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new PaymobPaymentResponse
            {
                Success = false,
                Message = $"Failed to create order: {responseContent}",
                RawResponse = responseContent
            };
        }

        var orderResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        return new PaymobPaymentResponse
        {
            Success = true,
            OrderId = orderResponse.GetProperty("id").GetString(),
            RawResponse = responseContent
        };
    }

    public async Task<PaymobPaymentResponse> CreatePaymentKeyAsync(
        string orderId,
        decimal amount,
        PaymobPaymentRequest request)
    {
        if (string.IsNullOrEmpty(_authToken))
        {
            await AuthenticateAsync();
        }

        var billingData = new
        {
            first_name = request.CustomerName.Split(' ')[0],
            last_name = request.CustomerName.Split(' ').Length > 1 
                ? string.Join(" ", request.CustomerName.Split(' ').Skip(1)) 
                : "",
            email = request.CustomerEmail,
            phone_number = request.CustomerPhone,
            apartment = "NA",
            floor = "NA",
            street = "NA",
            building = "NA",
            shipping_method = "NA",
            postal_code = "NA",
            city = "Cairo",
            country = "EG",
            state = "Cairo"
        };

        var paymentKeyRequest = new
        {
            auth_token = _authToken,
            amount_cents = (long)(amount * 100),
            expiration = 3600,
            order_id = orderId,
            billing_data = billingData,
            currency = request.Currency,
            integration_id = _settings.IntegrationId,
            lock_order_when_paid = false
        };

        var content = new StringContent(
            JsonSerializer.Serialize(paymentKeyRequest),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync("/acceptance/payment_keys", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new PaymobPaymentResponse
            {
                Success = false,
                Message = $"Failed to create payment key: {responseContent}",
                RawResponse = responseContent
            };
        }

        var paymentKeyResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        var paymentToken = paymentKeyResponse.GetProperty("token").GetString();

        var paymentUrl = $"https://accept.paymob.com/api/acceptance/iframes/{_settings.IFrameId}?payment_token={paymentToken}";

        return new PaymobPaymentResponse
        {
            Success = true,
            OrderId = orderId,
            PaymentToken = paymentToken,
            PaymentUrl = paymentUrl,
            RawResponse = responseContent
        };
    }

    public async Task<PaymobPaymentResponse> InitiatePaymentAsync(PaymobPaymentRequest request)
    {
        try
        {
            // Step 1: Create Order
            var orderResponse = await CreateOrderAsync(request.Amount, request.Currency);
            
            if (!orderResponse.Success || string.IsNullOrEmpty(orderResponse.OrderId))
            {
                return orderResponse;
            }

            // Step 2: Create Payment Key
            var paymentKeyResponse = await CreatePaymentKeyAsync(
                orderResponse.OrderId,
                request.Amount,
                request);

            return paymentKeyResponse;
        }
        catch (Exception ex)
        {
            return new PaymobPaymentResponse
            {
                Success = false,
                Message = $"Payment initiation failed: {ex.Message}"
            };
        }
    }

    public async Task<bool> ValidateCallbackHmacAsync(PaymobCallbackResponse callback)
    {
        if (callback?.Obj?.TransactionData?.Hmac == null)
        {
            return false;
        }

        var receivedHmac = callback.Obj.TransactionData.Hmac;
        var transactionData = callback.Obj.TransactionData;

        // Build the string for HMAC calculation
        var concatString = new StringBuilder();
        concatString.Append(callback.Obj.Id);
        concatString.Append(callback.Obj.OrderId);
        concatString.Append(transactionData.Amount);
        concatString.Append(transactionData.Currency);
        concatString.Append(transactionData.Success);
        concatString.Append(transactionData.IntegrationId);

        // Calculate HMAC
        var secretBytes = Encoding.UTF8.GetBytes(_settings.HmacSecret);
        var stringBytes = Encoding.UTF8.GetBytes(concatString.ToString());
        
        using var hmac = new HMACSHA512(secretBytes);
        var computedHash = hmac.ComputeHash(stringBytes);
        var computedHmac = BitConverter.ToString(computedHash).Replace("-", "").ToLower();

        return computedHmac == receivedHmac.ToLower();
    }

    public async Task<PaymobPaymentResponse> ProcessPaymentCallbackAsync(PaymobCallbackResponse callback)
    {
        try
        {
            // Validate HMAC
            if (!await ValidateCallbackHmacAsync(callback))
            {
                return new PaymobPaymentResponse
                {
                    Success = false,
                    Message = "Invalid HMAC signature"
                };
            }

            var isSuccess = callback.Obj?.TransactionData?.Success == "true";
            var transactionId = callback.Obj?.TransactionData?.Id;
            var orderId = callback.Obj?.OrderId;

            return new PaymobPaymentResponse
            {
                Success = isSuccess,
                OrderId = orderId,
                PaymentToken = transactionId,
                Message = isSuccess ? "Payment completed successfully" : "Payment failed",
                RawResponse = JsonSerializer.Serialize(callback)
            };
        }
        catch (Exception ex)
        {
            return new PaymobPaymentResponse
            {
                Success = false,
                Message = $"Callback processing failed: {ex.Message}"
            };
        }
    }

    public async Task<bool> RefundTransactionAsync(string transactionId, decimal amount)
    {
        if (string.IsNullOrEmpty(_authToken))
        {
            await AuthenticateAsync();
        }

        var refundRequest = new
        {
            auth_token = _authToken,
            transaction_id = transactionId,
            amount_cents = (long)(amount * 100)
        };

        var content = new StringContent(
            JsonSerializer.Serialize(refundRequest),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync("/acceptance/refund", content);
        
        return response.IsSuccessStatusCode;
    }
}
