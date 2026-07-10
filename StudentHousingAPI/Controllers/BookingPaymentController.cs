using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingPaymentController : BaseController
{
    private readonly IBookingPaymentService _bookingPaymentService;
    private readonly IPaymobService _paymobService;
    private readonly ILogger<BookingPaymentController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public BookingPaymentController(
        IBookingPaymentService bookingPaymentService,
        IPaymobService paymobService,
        ILogger<BookingPaymentController> logger,
        IUnitOfWork unitOfWork)
    {
        _bookingPaymentService = bookingPaymentService;
        _paymobService = paymobService;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    [HttpPost("initiate")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> InitiatePayment([FromBody] BookingPaymentRequest request)
    {
        var result = await _bookingPaymentService.InitiateBookingPaymentAsync(request);
        if (!result.Success)
            return BadRequest(new { Message = result.Message });
        
        return Ok(result);
    }
    
    /// <summary>
    /// Paymob transaction webhook callback endpoint.
    /// Paymob sends a POST with JSON containing the transaction object.
    /// Docs: https://developers.paymob.com/egypt/docs/transaction-webhooks
    /// </summary>
    [HttpPost("callback")]
    [AllowAnonymous]                              // Paymob calls this without auth
    [EnableCors("AllowPaymobWebhook")]            // server-to-server, no Origin header
    public async Task<IActionResult> PaymentCallback([FromBody] JsonElement body)
    {
        try
        {
            var rawBody = body.GetRawText();
            _logger.LogInformation("=== PAYMOB WEBHOOK CALLBACK START ===");
            _logger.LogInformation("Paymob POST callback received. Body length={Length}", rawBody.Length);
            _logger.LogInformation("Paymob POST callback raw body: {Body}", rawBody);

            string? orderId = null;
            string? transactionId = null;
            string? clientSecret = null;
            string? merchantOrderId = null;
            string? paymentKeyClaimsOrderId = null;
            string? objId = null;
            bool isSuccess = false;

            // Standard Paymob webhook format: { "obj": { ... }, "type": "TRANSACTION" }
            if (body.TryGetProperty("obj", out var obj))
            {
                // Extract obj.id
                if (obj.TryGetProperty("id", out var objIdProp))
                {
                    objId = objIdProp.ValueKind == JsonValueKind.Number
                        ? objIdProp.GetInt64().ToString()
                        : objIdProp.GetString();
                    _logger.LogInformation("Paymob webhook: obj.id = {ObjId}", objId);
                }

                // success can be bool or string
                if (obj.TryGetProperty("success", out var successProp))
                {
                    isSuccess = successProp.ValueKind == JsonValueKind.True
                        || (successProp.ValueKind == JsonValueKind.String
                            && successProp.GetString()?.ToLowerInvariant() == "true");
                    _logger.LogInformation("Paymob webhook: success = {IsSuccess}", isSuccess);
                }

                // Transaction ID
                if (obj.TryGetProperty("id", out var txnIdProp))
                    transactionId = txnIdProp.ValueKind == JsonValueKind.Number
                        ? txnIdProp.GetInt64().ToString()
                        : txnIdProp.GetString();
                _logger.LogInformation("Paymob webhook: transaction id = {TransactionId}", transactionId);

                // Client Secret (most reliable for matching)
                if (obj.TryGetProperty("client_secret", out var csProp))
                {
                    clientSecret = csProp.GetString();
                    var maskedClientSecret = !string.IsNullOrEmpty(clientSecret) && clientSecret.Length > 10
                        ? clientSecret.Substring(0, 10) + "..."
                        : clientSecret;
                    _logger.LogInformation("Paymob webhook: client_secret = {ClientSecret} (masked)", maskedClientSecret);
                }
                else
                {
                    _logger.LogWarning("Paymob webhook: client_secret NOT found in payload");
                }

                // Order ID — try obj.order.id first (legacy), then obj.payment_key_claims.order_id (intentions)
                if (obj.TryGetProperty("order", out var orderProp))
                {
                    _logger.LogInformation("Paymob webhook: order property found. Raw: {Order}", orderProp.GetRawText());

                    // Try merchant_order_id first (this is the intention ID we stored)
                    if (orderProp.TryGetProperty("merchant_order_id", out var moid) && moid.ValueKind != JsonValueKind.Null)
                    {
                        merchantOrderId = moid.GetString();
                        _logger.LogInformation("Paymob webhook: merchant_order_id = {MerchantOrderId}", merchantOrderId);
                    }
                    else
                    {
                        _logger.LogInformation("Paymob webhook: merchant_order_id NOT found in order object");
                    }

                    // Fallback to numeric order id
                    if (orderProp.TryGetProperty("id", out var oid))
                    {
                        orderId = oid.ValueKind == JsonValueKind.Number
                            ? oid.GetInt64().ToString()
                            : oid.GetString();
                        _logger.LogInformation("Paymob webhook: order.id = {OrderId}", orderId);
                    }
                    else
                    {
                        _logger.LogInformation("Paymob webhook: order.id NOT found in order object");
                    }
                }
                else
                {
                    _logger.LogWarning("Paymob webhook: order property NOT found in payload");
                }

                // Also try payment_key_claims for intentions API
                if (obj.TryGetProperty("payment_key_claims", out var pkc))
                {
                    _logger.LogInformation("Paymob webhook: payment_key_claims found. Raw: {Pkc}", pkc.GetRawText());
                    if (pkc.TryGetProperty("order_id", out var pkOrderId))
                    {
                        paymentKeyClaimsOrderId = pkOrderId.ValueKind == JsonValueKind.Number
                            ? pkOrderId.GetInt64().ToString()
                            : pkOrderId.GetString();
                        _logger.LogInformation("Paymob webhook: payment_key_claims.order_id = {PaymentKeyClaimsOrderId}", paymentKeyClaimsOrderId);
                    }
                    else
                    {
                        _logger.LogInformation("Paymob webhook: payment_key_claims.order_id NOT found");
                    }
                }
                else
                {
                    _logger.LogInformation("Paymob webhook: payment_key_claims NOT found in payload");
                }
            }
            else
            {
                _logger.LogWarning("Paymob webhook: obj property NOT found in payload");
            }

            // Flat format fallback (e.g. simplified callbacks or frontend-forwarded)
            if (!body.TryGetProperty("obj", out _))
            {
                _logger.LogInformation("Paymob webhook: trying flat format fallback");
                if (body.TryGetProperty("success", out var sp))
                    isSuccess = sp.ValueKind == JsonValueKind.True
                        || (sp.ValueKind == JsonValueKind.String && sp.GetString()?.ToLowerInvariant() == "true");

                if (body.TryGetProperty("id", out var txnId))
                    transactionId = txnId.ValueKind == JsonValueKind.Number
                        ? txnId.GetInt64().ToString()
                        : txnId.GetString();

                if (body.TryGetProperty("order_id", out var oid))
                    orderId = oid.ValueKind == JsonValueKind.Number
                        ? oid.GetInt64().ToString()
                        : oid.GetString();
                else if (body.TryGetProperty("orderId", out var oid2))
                    orderId = oid2.GetString();
                else if (body.TryGetProperty("merchant_order_id", out var moid))
                    orderId = moid.GetString();
            }

            // Extract metadata (BookingId and PaymentId) from webhook payload (recursive & case-insensitive)
            string? bookingIdFromMeta = null;
            string? paymentIdFromMeta = null;

            void FindMetadataRecursive(JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in element.EnumerateObject())
                    {
                        var name = property.Name.ToLowerInvariant();
                        if (name == "bookingid" || name == "booking_id")
                        {
                            if (property.Value.ValueKind == JsonValueKind.String)
                                bookingIdFromMeta = property.Value.GetString();
                            else if (property.Value.ValueKind == JsonValueKind.Number)
                                bookingIdFromMeta = property.Value.GetInt64().ToString();
                            else if (property.Value.ValueKind != JsonValueKind.Null)
                                bookingIdFromMeta = property.Value.ToString();
                        }
                        else if (name == "paymentid" || name == "payment_id")
                        {
                            if (property.Value.ValueKind == JsonValueKind.String)
                                paymentIdFromMeta = property.Value.GetString();
                            else if (property.Value.ValueKind == JsonValueKind.Number)
                                paymentIdFromMeta = property.Value.GetInt64().ToString();
                            else if (property.Value.ValueKind != JsonValueKind.Null)
                                paymentIdFromMeta = property.Value.ToString();
                        }
                        else
                        {
                            FindMetadataRecursive(property.Value);
                        }
                    }
                }
                else if (element.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.EnumerateArray())
                    {
                        FindMetadataRecursive(item);
                    }
                }
            }

            FindMetadataRecursive(body);

            // Summary of extracted fields
            _logger.LogInformation("=== PAYMOB WEBHOOK EXTRACTION SUMMARY ===");
            _logger.LogInformation("Extracted fields:");
            _logger.LogInformation("  - obj.id: {ObjId}", objId ?? "NULL");
            _logger.LogInformation("  - transaction id: {TransactionId}", transactionId ?? "NULL");
            _logger.LogInformation("  - order.id: {OrderId}", orderId ?? "NULL");
            _logger.LogInformation("  - merchant_order_id: {MerchantOrderId}", merchantOrderId ?? "NULL");
            _logger.LogInformation("  - payment_key_claims.order_id: {PaymentKeyClaimsOrderId}", paymentKeyClaimsOrderId ?? "NULL");
            _logger.LogInformation("  - client_secret: {ClientSecret} (masked)", !string.IsNullOrEmpty(clientSecret) && clientSecret.Length > 10 ? clientSecret.Substring(0, 10) + "..." : clientSecret ?? "NULL");
            _logger.LogInformation("  - success: {IsSuccess}", isSuccess);
            _logger.LogInformation("  - BookingId from Metadata: {BookingId}", bookingIdFromMeta ?? "NULL");
            _logger.LogInformation("  - PaymentId from Metadata: {PaymentId}", paymentIdFromMeta ?? "NULL");
            _logger.LogInformation("=== PAYMOB WEBHOOK EXTRACTION SUMMARY END ===");

            // Even if orderId is missing, try to find by transactionId
            if (string.IsNullOrWhiteSpace(orderId) && string.IsNullOrWhiteSpace(transactionId))
            {
                _logger.LogWarning("Paymob callback: both orderId and transactionId missing. Raw body logged at Debug level.");
                return Ok(new { Message = "Callback received but could not extract order or transaction ID" });
            }

            var result = await _bookingPaymentService.ProcessPaymentCallbackAsync(
                orderId:       orderId ?? string.Empty,
                transactionId: transactionId ?? string.Empty,
                clientSecret:  clientSecret ?? string.Empty,
                isSuccess:     isSuccess,
                bookingId:     bookingIdFromMeta,
                paymentId:     paymentIdFromMeta,
                merchantOrderId: merchantOrderId ?? paymentKeyClaimsOrderId);

            _logger.LogInformation(
                "POST callback processed. Success={Success}, Message={Message}",
                result.Success, result.Message);

            _logger.LogInformation("=== PAYMOB WEBHOOK CALLBACK END ===");

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Paymob POST callback");
            _logger.LogInformation("=== PAYMOB WEBHOOK CALLBACK END (ERROR) ===");
            return Ok(new { Message = "Callback received" }); // Always 200 to prevent Paymob retries
        }
    }

    /// <summary>
    /// Paymob redirect callback (GET) — Paymob redirects the user here after payment.
    /// Process the payment FIRST, then redirect the user to the frontend result page.
    /// Paymob appends query params: success, id (transaction id), order (intention/order id), etc.
    /// </summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    [EnableCors("AllowPaymobWebhook")]
    public async Task<IActionResult> PaymentCallbackRedirect(
        [FromQuery] bool success,
        [FromQuery] string? id,
        [FromQuery] string? order,
        [FromQuery] string? client_secret)   // Paymob sometimes appends this to the redirect URL
    {
        _logger.LogInformation(
            "Paymob GET redirect callback. OrderId={OrderId}, TransactionId={TxnId}, ClientSecret={HasCs}, Success={IsSuccess}",
            order, id, !string.IsNullOrEmpty(client_secret), success);

        if (!string.IsNullOrWhiteSpace(order) || !string.IsNullOrWhiteSpace(id))
        {
            try
            {
                // Pass every available identifier so the multi-strategy lookup in the service
                // has the best chance of finding the PaymentTransaction record.
                // - orderId  = Paymob numeric order ID (also try as intention ID via GetByOrderOrIntentionIdAsync)
                // - transactionId = Paymob numeric transaction ID (also try as intention ID)
                // - clientSecret = if Paymob appended it to the redirect URL
                var result = await _bookingPaymentService.ProcessPaymentCallbackAsync(
                    orderId:       order ?? id ?? string.Empty,
                    transactionId: id    ?? string.Empty,
                    clientSecret:  client_secret ?? string.Empty,
                    isSuccess:     success);

                _logger.LogInformation(
                    "GET callback processed. OrderId={OrderId}, Success={Success}, Message={Message}",
                    order, result.Success, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing GET callback for OrderId={OrderId}", order);
            }
        }
        else
        {
            _logger.LogWarning("Paymob GET callback received without order or id param. success={Success}", success);
        }

        // Redirect to frontend with result
        var frontendUrl = success
            ? "https://unistay-shbs.vercel.app/payment-success"
            : "https://unistay-shbs.vercel.app/payment-failed";

        return Redirect(frontendUrl);
    }

    /// <summary>
    /// Frontend-initiated payment confirmation.
    /// Called by the frontend when the user lands on /payment-success or /payment-failed
    /// with Paymob query params. This is a safety net in case the GET redirect callback
    /// was not hit (e.g. Paymob redirected directly to the frontend URL).
    /// </summary>
    [HttpPost("confirm")]
    [AllowAnonymous]
    [EnableCors("AllowPaymobWebhook")]
    public async Task<IActionResult> ConfirmPayment(
        [FromQuery] string? order,
        [FromQuery] string? id,
        [FromQuery] bool success = false,
        [FromQuery] string? bookingId = null,
        [FromQuery] string? paymentId = null)
    {
        _logger.LogInformation(
            "Frontend confirm call. OrderId={OrderId}, TransactionId={TxnId}, Success={IsSuccess}, BookingId={BookingId}, PaymentId={PaymentId}",
            order, id, success, bookingId, paymentId);

        if (string.IsNullOrWhiteSpace(order) && string.IsNullOrWhiteSpace(bookingId) && string.IsNullOrWhiteSpace(paymentId))
            return BadRequest(new { Message = "At least one reference parameter (order, bookingId, or paymentId) is required" });

        var result = await _bookingPaymentService.ProcessPaymentCallbackAsync(
            orderId:       order ?? string.Empty,
            transactionId: id ?? string.Empty,
            clientSecret:  string.Empty, // clientSecret not available in confirm endpoint
            isSuccess:     success,
            bookingId:     bookingId,
            paymentId:     paymentId);

        return Ok(result);
    }

    [HttpPost("sync-pending")]
    [AllowAnonymous]
    public async Task<IActionResult> SyncPendingPayments()
    {
        _logger.LogInformation("Manual trigger for SyncPendingPayments received.");
        var updatedCount = await _bookingPaymentService.SyncPendingPaymentsAsync();
        return Ok(new { Message = $"Sync completed. {updatedCount} transactions updated.", UpdatedCount = updatedCount });
    }

    [HttpPost("complete/{paymentId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CompleteWorkflow(Guid paymentId)
    {
        var result = await _bookingPaymentService.CompleteBookingWorkflowAsync(paymentId);
        if (!result.Success)
            return BadRequest(new { Message = result.Message });
        
        return Ok(result);
    }

    /// <summary>
    /// Manual contract generation endpoint for testing/debugging
    /// Allows manual triggering of contract generation for a completed payment
    /// </summary>
    [HttpPost("generate-contract/{paymentId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GenerateContractManually(Guid paymentId)
    {
        _logger.LogInformation($"Manual contract generation requested for payment {paymentId}");
        var result = await _bookingPaymentService.CompleteBookingWorkflowAsync(paymentId);
        
        if (!result.Success)
        {
            _logger.LogError($"Manual contract generation failed for payment {paymentId}: {result.Message}");
            return BadRequest(new { Message = result.Message });
        }
        
        _logger.LogInformation($"Manual contract generation succeeded for payment {paymentId}. ContractId: {result.ContractId}");
        return Ok(result);
    }

    /// <summary>
    /// Recovery endpoint to retry contract generation for completed payments without contracts
    /// This handles cases where payment succeeded but contract generation failed
    /// </summary>
    [HttpPost("retry-contract/{paymentId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RetryContractGeneration(Guid paymentId)
    {
        _logger.LogInformation($"Contract generation retry requested for payment {paymentId}");
        
        try
        {
            // Verify payment exists and is completed
            var payment = await _unitOfWork.Payments.GetAsync(paymentId);
            if (payment == null)
            {
                _logger.LogWarning($"Payment not found for retry: {paymentId}");
                return NotFound(new { Message = "Payment not found" });
            }
            
            _logger.LogInformation($"Payment found: {paymentId}, Status: {payment.PaymentStatus}");
            
            if (payment.PaymentStatus != PaymentStatus.Completed)
            {
                _logger.LogWarning($"Payment {paymentId} is not completed. Current status: {payment.PaymentStatus}");
                return BadRequest(new { Message = $"Payment is not completed. Current status: {payment.PaymentStatus}" });
            }
            
            // Check if contract already exists
            var existingContract = await _unitOfWork.Contracts.GetByBookingIdAsync(payment.BookingId);
            if (existingContract != null)
            {
                _logger.LogInformation($"Contract already exists for payment {paymentId}. ContractId: {existingContract.ContractId}");
                return Ok(new { 
                    Message = "Contract already exists", 
                    ContractId = existingContract.ContractId,
                    ContractPdfUrl = existingContract.OriginalContractPdfPath
                });
            }
            
            _logger.LogInformation($"No contract exists for payment {paymentId}. Proceeding with contract generation");
            
            // Get booking details
            var booking = await _unitOfWork.Bookings.GetAsync(payment.BookingId);
            if (booking == null)
            {
                _logger.LogError($"Booking not found for payment {paymentId}");
                return NotFound(new { Message = "Booking not found" });
            }
            
            // Invoke contract workflow
            await _bookingPaymentService.CompleteBookingWorkflowAsync(paymentId);
            
            // Verify contract was created
            var newContract = await _unitOfWork.Contracts.GetByBookingIdAsync(payment.BookingId);
            if (newContract != null)
            {
                _logger.LogInformation($"Contract generation retry succeeded for payment {paymentId}. ContractId: {newContract.ContractId}");
                return Ok(new { 
                    Message = "Contract generated successfully", 
                    ContractId = newContract.ContractId,
                    ContractPdfUrl = newContract.OriginalContractPdfPath
                });
            }
            else
            {
                _logger.LogError($"Contract generation retry failed for payment {paymentId}. Contract still not found");
                return BadRequest(new { Message = "Contract generation failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during contract generation retry for payment {paymentId}");
            return StatusCode(500, new { Message = $"Internal server error: {ex.Message}" });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DEBUG ENDPOINT — development / testing only.
    // Bypasses Paymob entirely and calls ProcessPaymentCallbackAsync directly
    // using a real booking that already exists in the database.
    //
    // Usage (Swagger / curl):
    //   POST /api/BookingPayment/debug/simulate-callback?bookingId=<guid>&success=true
    //
    // How to find a valid bookingId:
    //   SELECT TOP 1 BookingId, BookingStatus FROM Bookings
    //   WHERE BookingStatus = 0  -- 0 = PendingPayment
    //
    // The endpoint looks up the PaymentTransaction for that booking and feeds
    // its stored IDs directly into ProcessPaymentCallbackAsync, so the full
    // instrumented flow runs exactly as it would for a real Paymob callback.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("debug/simulate-callback")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DebugSimulateCallback(
        [FromQuery] Guid bookingId,
        [FromQuery] bool success = true)
    {
        _logger.LogWarning("=== DEBUG simulate-callback START: BookingId={BookingId} Success={Success} ===", bookingId, success);

        // 1. Load the payment for this booking
        var payment = await _unitOfWork.Payments.GetByBookingIdAsync(bookingId);
        if (payment == null)
        {
            _logger.LogError("DEBUG: No Payment found for BookingId={BookingId}", bookingId);
            return NotFound(new
            {
                Step = "LoadPayment",
                Message = $"No payment record found for booking {bookingId}. " +
                          "Make sure you have called POST /api/BookingPayment/initiate first."
            });
        }

        _logger.LogInformation("DEBUG: Payment found. PaymentId={PaymentId} Status={Status}", payment.PaymentId, payment.PaymentStatus);

        // 2. Load the PaymentTransaction so we have real Paymob IDs
        var txn = await _unitOfWork.PaymentTransactions.GetByPaymentIdAsync(payment.PaymentId);
        if (txn == null)
        {
            _logger.LogError("DEBUG: No PaymentTransaction found for PaymentId={PaymentId}", payment.PaymentId);
            return NotFound(new
            {
                Step = "LoadPaymentTransaction",
                Message = $"No PaymentTransaction record found for payment {payment.PaymentId}. " +
                          "Make sure initiate completed successfully and saved a PaymentTransaction row."
            });
        }

        _logger.LogInformation(
            "DEBUG: PaymentTransaction found. TxnId={TxnId} OrderId={OrderId} IntentionId={IntentionId} ClientSecret={CS}",
            txn.TransactionId, txn.PaymobOrderId, txn.PaymobIntentionId,
            string.IsNullOrEmpty(txn.ClientSecret) ? "NULL" : txn.ClientSecret[..Math.Min(15, txn.ClientSecret.Length)] + "...");

        // 3. Call the real processing method with the stored IDs — same path as a live Paymob callback
        var result = await _bookingPaymentService.ProcessPaymentCallbackAsync(
            orderId:         txn.PaymobOrderId       ?? string.Empty,
            transactionId:   txn.PaymobTransactionId ?? txn.PaymobOrderId ?? string.Empty,
            clientSecret:    txn.ClientSecret        ?? string.Empty,
            isSuccess:       success,
            bookingId:       bookingId.ToString(),
            paymentId:       payment.PaymentId.ToString(),
            merchantOrderId: txn.PaymobIntentionId   ?? string.Empty);

        _logger.LogWarning("=== DEBUG simulate-callback END: Success={Success} Message={Message} ===", result.Success, result.Message);

        // 4. Return a diagnostic snapshot alongside the service result
        var bookingAfter  = await _unitOfWork.Bookings.GetAsync(bookingId);
        var paymentAfter  = await _unitOfWork.Payments.GetAsync(payment.PaymentId);

        return Ok(new
        {
            SimulationResult  = result,
            DiagnosticSnapshot = new
            {
                PaymentStatusBefore = payment.PaymentStatus.ToString(),
                PaymentStatusAfter  = paymentAfter?.PaymentStatus.ToString() ?? "NOT FOUND",
                BookingStatusAfter  = bookingAfter?.BookingStatus.ToString()  ?? "NOT FOUND",
                PaymentId           = payment.PaymentId,
                BookingId           = bookingId,
                PaymobOrderId       = txn.PaymobOrderId,
                PaymobIntentionId   = txn.PaymobIntentionId,
                ClientSecretPresent = !string.IsNullOrEmpty(txn.ClientSecret)
            },
            Instructions = new
            {
                NextStep      = result.Success ? "Check logs for ╠══ lines to trace each step" : "Check logs for ╠══ EXCEPTION or ╠══ FAILED lines",
                LogFilter     = "Search logs for: ProcessPaymentCallbackAsync",
                KeyLogMarkers = new[] { "╔══ ENTRY", "╠══ STEP:", "╠══ EXCEPTION", "╠══ FAILED", "╠══ EF ChangeTracker", "╚══ EXIT" }
            }
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DEBUG: Test Paymob auth and transaction retrieval without a real payment.
    //
    // POST /api/BookingPayment/debug/test-paymob-auth
    //   → Tests POST /api/auth/tokens with your configured LegacyApiKey
    //
    // POST /api/BookingPayment/debug/test-transaction?transactionId=493478206
    //   → Tests GET /api/acceptance/transactions/{id} using Bearer token
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("debug/test-paymob-auth")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DebugTestPaymobAuth()
    {
        _logger.LogWarning("=== DEBUG test-paymob-auth START ===");
        var result = await _paymobService.GetBearerTokenAsync();
        _logger.LogWarning("=== DEBUG test-paymob-auth END: Success={S} ===", result.Success);

        return Ok(new
        {
            Success      = result.Success,
            Message      = result.Message,
            TokenPresent = !string.IsNullOrEmpty(result.Token),
            TokenPrefix  = result.Token?.Length > 20
                ? result.Token[..20] + "..."
                : result.Token,
            Instructions = result.Success
                ? "Auth OK. Now test /debug/test-transaction?transactionId=YOUR_NUMERIC_ID"
                : "Auth FAILED. Set Paymob:LegacyApiKey in appsettings. " +
                  "Get it from: Paymob Dashboard → Settings → Account Info → API Key"
        });
    }

    [HttpPost("debug/test-transaction")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DebugTestTransaction([FromQuery] string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            return BadRequest(new { Message = "transactionId query param is required. Use the numeric id from ?id= in the Paymob redirect." });

        _logger.LogWarning("=== DEBUG test-transaction START: TransactionId={Id} ===", transactionId);
        var json = await _paymobService.GetTransactionDetailsJsonAsync(transactionId);

        if (json == null)
            return Ok(new
            {
                Success       = false,
                TransactionId = transactionId,
                Message       = "Transaction retrieval failed. Check logs for GetTransactionDetailsAsync.",
            });

        string? numericOrderId  = null;
        string? merchantOrderId = null;
        bool    txnSuccess      = false;
        long    amountCents     = 0;

        if (json.Value.TryGetProperty("order", out var orderEl))
        {
            if (orderEl.TryGetProperty("id", out var oid))
                numericOrderId = oid.ValueKind == System.Text.Json.JsonValueKind.Number
                    ? oid.GetInt64().ToString() : oid.GetString();
            if (orderEl.TryGetProperty("merchant_order_id", out var moid)
                && moid.ValueKind != System.Text.Json.JsonValueKind.Null)
                merchantOrderId = moid.GetString();
        }
        if (json.Value.TryGetProperty("success",      out var sp)) txnSuccess  = sp.ValueKind == System.Text.Json.JsonValueKind.True;
        if (json.Value.TryGetProperty("amount_cents", out var ac)) amountCents = ac.GetInt64();

        _logger.LogWarning("=== DEBUG test-transaction END ===");

        return Ok(new
        {
            Success         = true,
            TransactionId   = transactionId,
            PaymobSuccess   = txnSuccess,
            AmountEGP       = amountCents / 100m,
            NumericOrderId  = numericOrderId,
            MerchantOrderId = merchantOrderId,
            Note = "MerchantOrderId should match pi_test_xxx in PaymentTransactions.PaymobIntentionId"
        });
    }
}
