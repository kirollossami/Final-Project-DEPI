using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Business.Services;

/// <summary>
/// Handles payment initiation and Paymob callback ONLY.
/// Responsibilities after successful payment:
///   1. Mark Payment as Completed.
///   2. Mark booking as WaitingForContract.
///   3. Generate a PaymentReceived receipt for the student.
///   4. Credit the full payment amount to the Admin balance.
///
/// Does NOT create escrow, does NOT create a contract.
/// Escrow is created by AdminApprovalService.UploadContractAsync.
/// Contract is created manually by the admin via AdminApprovalService.UploadContractAsync.
/// </summary>
public class BookingPaymentService : IBookingPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymobService _paymobService;
    private readonly IReceiptService _receiptService;
    private readonly INotificationService _notificationService;
    private readonly IPaymentHistoryService _paymentHistoryService;
    private readonly IBalanceService _balanceService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<BookingPaymentService> _logger;
    private const decimal PlatformFeePercentage = 5.0m;

    public BookingPaymentService(
        IUnitOfWork unitOfWork,
        IPaymobService paymobService,
        IReceiptService receiptService,
        INotificationService notificationService,
        IPaymentHistoryService paymentHistoryService,
        IBalanceService balanceService,
        UserManager<User> userManager,
        ILogger<BookingPaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _paymobService = paymobService;
        _receiptService = receiptService;
        _notificationService = notificationService;
        _paymentHistoryService = paymentHistoryService;
        _balanceService = balanceService;
        _userManager = userManager;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Initiate payment (unchanged)
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<BookingPaymentResponse> InitiateBookingPaymentAsync(BookingPaymentRequest request)
    {
        try
        {
            var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Step 1: Get booking details
                var booking = await _unitOfWork.Bookings.GetAsync(request.BookingId);
                if (booking == null)
                    return new BookingPaymentResponse { Success = false, Message = "Booking not found" };

                // Allow retrying payment from PendingPayment state
                var payableStatuses = new[]
                {
                    BookingStatus.PendingPayment
                };

                if (!payableStatuses.Contains(booking.BookingStatus))
                    return new BookingPaymentResponse { Success = false, Message = $"Booking cannot be paid. Current status: {booking.BookingStatus}" };

                // Guard: Paymob must be configured
                if (string.IsNullOrWhiteSpace(request.CustomerEmail))
                    return new BookingPaymentResponse { Success = false, Message = "Missing required payment details (CustomerEmail)" };

                // Step 2: Update booking status to PendingPayment
                booking.BookingStatus = BookingStatus.PendingPayment;
                booking.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Bookings.Update(booking);

                // Step 3: Get or create payment record (reuse if already exists for this booking)
                var payment = await _unitOfWork.Payments.GetByBookingIdAsync(booking.BookingId);
                if (payment == null)
                {
                    payment = new Payment
                    {
                        PaymentId = Guid.NewGuid(),
                        BookingId = booking.BookingId,
                        Amount = booking.TotalPrice,
                        PaymentMethod = PaymentMethod.Card,
                        PaymentStatus = PaymentStatus.Pending,
                        PaymentDate = DateTime.UtcNow,
                        TransactionId = Guid.NewGuid().ToString()
                    };
                    await _unitOfWork.Payments.Insert(payment);
                }
                else
                {
                    // Reset the existing payment for retry
                    payment.PaymentStatus = PaymentStatus.Pending;
                    payment.PaymentDate = DateTime.UtcNow;
                    payment.TransactionId = Guid.NewGuid().ToString();
                    payment.CompletedAt = null;
                    await _unitOfWork.Payments.Update(payment);
                }
                await _unitOfWork.SaveChangesAsync();

                // Step 4: Initiate Paymob payment
                var paymobRequest = new PaymobPaymentRequest
                {
                    Amount = booking.TotalPrice,
                    Currency = "EGP",
                    CustomerName = request.CustomerName,
                    CustomerEmail = request.CustomerEmail,
                    CustomerPhone = request.CustomerPhone,
                    Description = request.Description ?? $"Booking Payment for {booking.BookingId}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "BookingId", booking.BookingId.ToString() },
                        { "PaymentId", payment.PaymentId.ToString() }
                    }
                };

                var paymobResponse = await _paymobService.InitiatePaymentAsync(paymobRequest);

                if (!paymobResponse.Success || string.IsNullOrEmpty(paymobResponse.PaymentUrl))
                    return new BookingPaymentResponse { Success = false, Message = $"Payment initiation failed: {paymobResponse.Message}" };

                // Step 5: Create payment transaction record
                var paymentTransaction = new PaymentTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    PaymentId = payment.PaymentId,
                    PaymobOrderId = paymobResponse.OrderId ?? string.Empty,
                    PaymobIntentionId = paymobResponse.IntentionId ?? string.Empty,
                    Amount = booking.TotalPrice,
                    Currency = "EGP",
                    GatewayStatus = PaymentGatewayStatus.Pending,
                    PaymentToken = paymobResponse.PaymentToken,
                    PaymentUrl = paymobResponse.PaymentUrl,
                    RawResponse = paymobResponse.RawResponse
                };

                await _unitOfWork.PaymentTransactions.Insert(paymentTransaction);
                await _unitOfWork.SaveChangesAsync();

                // Step 6: Update booking status to PendingPayment (stays in this state until payment completes)
                booking.BookingStatus = BookingStatus.PendingPayment;
                await _unitOfWork.Bookings.Update(booking);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Payment initiated successfully for booking {booking.BookingId}.");

                // Non-transactional notification after commit
                _ = Task.Run(async () => await _notificationService.SendRealTimeNotificationAsync(
                    booking.Student?.UserId ?? string.Empty,
                    "Payment initiated for your booking. Please complete the payment to proceed.",
                    "PaymentInitiated"));

                // Return success; payment history will be recorded after transaction
                return new BookingPaymentResponse
                {
                    Success = true,
                    Message = "Payment initiated successfully",
                    PaymentId = payment.PaymentId,
                    PaymentUrl = paymobResponse.PaymentUrl
                };
            });

            // After transaction has completed, record payment history in a non-fatal manner
            try
            {
                var paymentRec = await _unitOfWork.Payments.GetByBookingIdAsync(request.BookingId);
                if (paymentRec != null)
                {
                    var studentRec = await _unitOfWork.Students.GetAsync((await _unitOfWork.Payments.GetByBookingIdAsync(request.BookingId))?.Booking?.StudentId ?? Guid.Empty);
                    var historyMetadata = new Dictionary<string, object>
                    {
                        { "PaymobOrderId", (await _unitOfWork.PaymentTransactions.GetByPaymobOrderIdAsync(""))?.PaymobOrderId ?? string.Empty },
                        { "PaymentMethod", PaymentMethod.Card.ToString() }
                    };

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _paymentHistoryService.RecordPaymentEventAsync(
                                paymentRec.PaymentId,
                                request.BookingId,
                                null,
                                studentRec?.UserId ?? string.Empty,
                                "PaymentInitiated",
                                $"Payment initiated for booking. Amount: {paymentRec.Amount} EGP. Awaiting completion.",
                                paymentRec.Amount,
                                BookingStatus.PendingPayment.ToString(),
                                BookingStatus.PendingPayment.ToString(),
                                "System",
                                "System",
                                metadata: historyMetadata);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Non-fatal: failed to record payment history after initiation");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Non-fatal: failed to kick off payment history recording after transaction");
            }

            // Return the transaction result to the caller
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating booking payment");
            // Unwrap DbUpdateException to expose the actual SQL error
            var innerMessage = ex.InnerException?.InnerException?.Message
                            ?? ex.InnerException?.Message
                            ?? ex.Message;
            return new BookingPaymentResponse { Success = false, Message = $"An error occurred: {innerMessage}" };
        }
    }

   
    public async Task<BookingPaymentResponse> ProcessPaymentCallbackAsync(
        string orderId, 
        string transactionId, 
        string clientSecret, 
        bool isSuccess, 
        string? bookingId = null, 
        string? paymentId = null,
        string? merchantOrderId = null)
    {
        string currentStep = "MethodEntry";
        _logger.LogInformation(
            "╔══ ProcessPaymentCallbackAsync ENTRY ══ Step={Step} | OrderId={OrderId} | TxnId={TxnId} | ClientSecret={CS} | Success={IsSuccess} | BookingId={BookingId} | PaymentId={PaymentId} | MerchantOrderId={MerchantOrderId}",
            currentStep, orderId, transactionId,
            string.IsNullOrEmpty(clientSecret) ? "NULL" : clientSecret[..Math.Min(15, clientSecret.Length)] + "...",
            isSuccess, bookingId, paymentId, merchantOrderId);

        // Find PaymentTransaction locally first (read-only lookup outside transaction to check if we need fallback)
        PaymentTransaction? paymentTxn = null;

        // ── Multi-strategy lookup: find the PaymentTransaction row ───────────────
        // Strategy 1: clientSecret / PaymentToken  (POST webhook with client_secret)
        if (!string.IsNullOrWhiteSpace(clientSecret))
            paymentTxn = await _unitOfWork.PaymentTransactions.GetByClientSecretAsync(clientSecret);

        // Strategy 2: PaymobTransactionId  (numeric transaction id from Paymob)
        if (paymentTxn == null && !string.IsNullOrWhiteSpace(transactionId))
            paymentTxn = await _unitOfWork.PaymentTransactions.GetByPaymobTransactionIdAsync(transactionId);

        // Strategy 3: PaymobOrderId OR PaymobIntentionId in one query —
        //   Covers both old (numeric order id) and new (pi_test_... intention id) formats.
        if (paymentTxn == null && !string.IsNullOrWhiteSpace(orderId))
            paymentTxn = await _unitOfWork.PaymentTransactions.GetByOrderOrIntentionIdAsync(orderId);

        // Strategy 3b: Match using merchantOrderId (which contains the Intention ID e.g. pi_test_...)
        if (paymentTxn == null && !string.IsNullOrWhiteSpace(merchantOrderId))
            paymentTxn = await _unitOfWork.PaymentTransactions.GetByOrderOrIntentionIdAsync(merchantOrderId);

        // Strategy 4: transactionId treated as an intention/order id
        //   (Paymob GET redirect uses ?order=<intention_id> in some flows)
        if (paymentTxn == null && !string.IsNullOrWhiteSpace(transactionId) && transactionId != orderId)
            paymentTxn = await _unitOfWork.PaymentTransactions.GetByOrderOrIntentionIdAsync(transactionId);

        // Strategy 5: explicit paymentId passed from metadata
        if (paymentTxn == null && !string.IsNullOrWhiteSpace(paymentId) && Guid.TryParse(paymentId, out var pGuid))
            paymentTxn = await _unitOfWork.PaymentTransactions.GetByPaymentIdAsync(pGuid);

        // Strategy 6: lookup via bookingId metadata -> Payment -> PaymentTransaction
        if (paymentTxn == null && !string.IsNullOrWhiteSpace(bookingId) && Guid.TryParse(bookingId, out var bGuid))
        {
            var pRec = await _unitOfWork.Payments.GetByBookingIdAsync(bGuid);
            if (pRec != null)
                paymentTxn = await _unitOfWork.PaymentTransactions.GetByPaymentIdAsync(pRec.PaymentId);
        }

        // ── Fallback to Paymob API Inquiry (outside DB transaction) ──────────────────
        if (paymentTxn == null && !string.IsNullOrWhiteSpace(transactionId))
        {
            _logger.LogInformation("No local PaymentTransaction found. Querying Paymob API for Transaction ID: {TransactionId}", transactionId);
            try
            {
                var txnDetails = await _paymobService.GetTransactionDetailsJsonAsync(transactionId);
                if (txnDetails.HasValue)
                {
                    string? paymobClientSecret = null;
                    if (txnDetails.Value.TryGetProperty("client_secret", out var csProp))
                        paymobClientSecret = csProp.GetString();
                    else if (txnDetails.Value.TryGetProperty("order", out var orderProp) && orderProp.TryGetProperty("client_secret", out var ocsProp))
                        paymobClientSecret = ocsProp.GetString();
                    else if (txnDetails.Value.TryGetProperty("payment_key_claims", out var pkcProp) && pkcProp.TryGetProperty("client_secret", out var pkcsProp))
                        paymobClientSecret = pkcsProp.GetString();

                    if (!string.IsNullOrWhiteSpace(paymobClientSecret))
                    {
                        _logger.LogInformation("Resolved client secret from Paymob API: {ClientSecret}", paymobClientSecret);
                        paymentTxn = await _unitOfWork.PaymentTransactions.GetByClientSecretAsync(paymobClientSecret);
                    }

                    if (paymentTxn == null)
                    {
                        // Try metadata fields in the retrieved transaction details
                        string? bookingIdStr = null;
                        string? paymentIdStr = null;

                        void ExtractFromExtra(JsonElement element)
                        {
                            if (element.TryGetProperty("extra", out var extra) && extra.ValueKind == JsonValueKind.Object)
                            {
                                if (extra.TryGetProperty("BookingId", out var bId)) bookingIdStr = bId.GetString();
                                if (extra.TryGetProperty("PaymentId", out var pId)) paymentIdStr = pId.GetString();
                            }
                            if (element.TryGetProperty("extras", out var extras) && extras.ValueKind == JsonValueKind.Object)
                            {
                                if (extras.TryGetProperty("BookingId", out var bId)) bookingIdStr = bId.GetString();
                                if (extras.TryGetProperty("PaymentId", out var pId)) paymentIdStr = pId.GetString();
                            }
                        }

                        ExtractFromExtra(txnDetails.Value);
                        if (txnDetails.Value.TryGetProperty("order", out var orderEl)) ExtractFromExtra(orderEl);
                        if (txnDetails.Value.TryGetProperty("payment_key_claims", out var pkcEl)) ExtractFromExtra(pkcEl);

                        if (!string.IsNullOrWhiteSpace(paymentIdStr) && Guid.TryParse(paymentIdStr, out var paymentGuid))
                        {
                            _logger.LogInformation("Resolved PaymentId from Paymob metadata: {PaymentId}", paymentGuid);
                            paymentTxn = await _unitOfWork.PaymentTransactions.GetByPaymentIdAsync(paymentGuid);
                        }
                        else if (!string.IsNullOrWhiteSpace(bookingIdStr) && Guid.TryParse(bookingIdStr, out var bookingGuid))
                        {
                            _logger.LogInformation("Resolved BookingId from Paymob metadata: {BookingId}", bookingGuid);
                            var paymentRec = await _unitOfWork.Payments.GetByBookingIdAsync(bookingGuid);
                            if (paymentRec != null)
                            {
                                paymentTxn = await _unitOfWork.PaymentTransactions.GetByPaymentIdAsync(paymentRec.PaymentId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing Paymob transaction inquiry fallback for transaction {TransactionId}", transactionId);
            }
        }

        // ── Fallback 2 to Paymob Intention API Inquiry (outside DB transaction) ──────────────────
        var intentionSearchId = merchantOrderId ?? (orderId.StartsWith("pi_") ? orderId : null);
        if (paymentTxn == null && !string.IsNullOrWhiteSpace(intentionSearchId))
        {
            _logger.LogInformation("No local PaymentTransaction found. Querying Paymob Intention API for Intention ID: {IntentionId}", intentionSearchId);
            try
            {
                var intentionDetails = await _paymobService.GetIntentionDetailsAsync(intentionSearchId);
                if (intentionDetails.HasValue)
                {
                    string? paymobClientSecret = null;
                    if (intentionDetails.Value.TryGetProperty("client_secret", out var csProp))
                        paymobClientSecret = csProp.GetString();

                    if (!string.IsNullOrWhiteSpace(paymobClientSecret))
                    {
                        _logger.LogInformation("Resolved client secret from Paymob Intention API: {ClientSecret}", paymobClientSecret);
                        paymentTxn = await _unitOfWork.PaymentTransactions.GetByClientSecretAsync(paymobClientSecret);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing Paymob intention details inquiry fallback for intention {IntentionId}", intentionSearchId);
            }
        }

        _logger.LogInformation("── Lookup complete. paymentTxn={Found} ──", paymentTxn != null ? $"FOUND TxnId={paymentTxn.TransactionId} PaymentId={paymentTxn.PaymentId}" : "NOT FOUND");

        if (paymentTxn == null)
        {
            _logger.LogWarning("No PaymentTransaction found locally or via Paymob API. OrderId={OrderId}, TxnId={TxnId}", orderId, transactionId);
            return new BookingPaymentResponse { Success = false, Message = "Payment transaction not found" };
        }

        var receiptRequests = new List<ReceiptGenerationRequest>();
        BookingPaymentResponse? phaseOneResult = null;
        var targetTxnId = paymentTxn.TransactionId;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                currentStep = "Re-fetching PaymentTransaction inside transaction";
                _logger.LogInformation("╠══ STEP: {Step}", currentStep);

                var dbTxn = await _unitOfWork.PaymentTransactions.GetAsync(targetTxnId);
                if (dbTxn == null)
                {
                    _logger.LogError("╠══ FAILED: PaymentTransaction {TxnId} not found in DB during transaction", targetTxnId);
                    phaseOneResult = new BookingPaymentResponse { Success = false, Message = "Payment transaction not found in DB" };
                    return;
                }

                _logger.LogInformation("╠══ OK: PaymentTransaction. TxnId={TxnId} PaymentId={PaymentId} GatewayStatus={Status}", dbTxn.TransactionId, dbTxn.PaymentId, dbTxn.GatewayStatus);

                // 2. Find Payment — check idempotency BEFORE mutating dbTxn
                currentStep = "Loading Payment entity (pre-idempotency check)";
                _logger.LogInformation("╠══ STEP: {Step} | PaymentId={PaymentId}", currentStep, dbTxn.PaymentId);
                var payment = await _unitOfWork.Payments.GetAsync(dbTxn.PaymentId);
                if (payment == null)
                {
                    _logger.LogError("╠══ FAILED: Payment {PaymentId} not found", dbTxn.PaymentId);
                    phaseOneResult = new BookingPaymentResponse { Success = false, Message = "Payment record not found" };
                    return;
                }
                _logger.LogInformation("╠══ OK: Payment. Id={PaymentId} BookingId={BookingId} CurrentStatus={Status}", payment.PaymentId, payment.BookingId, payment.PaymentStatus);

                // 3. Find Booking
                currentStep = "Loading Booking entity (pre-idempotency check)";
                _logger.LogInformation("╠══ STEP: {Step} | BookingId={BookingId}", currentStep, payment.BookingId);
                var booking = await _unitOfWork.Bookings.GetAsync(payment.BookingId);
                if (booking == null)
                {
                    _logger.LogError("╠══ FAILED: Booking {BookingId} not found", payment.BookingId);
                    phaseOneResult = new BookingPaymentResponse { Success = false, Message = "Booking not found" };
                    return;
                }
                _logger.LogInformation("╠══ OK: Booking. Id={BookingId} CurrentStatus={Status}", booking.BookingId, booking.BookingStatus);

                // Idempotency — return early before touching dbTxn if already completed
                if (payment.PaymentStatus == PaymentStatus.Completed)
                {
                    _logger.LogInformation("╠══ IDEMPOTENCY: Payment {PaymentId} already Completed. BookingStatus={Status} — no writes, returning.", payment.PaymentId, booking.BookingStatus);
                    if (booking.BookingStatus == BookingStatus.PendingPayment)
                    {
                        booking.BookingStatus = BookingStatus.WaitingForContract;
                        booking.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.Bookings.Update(booking);
                    }
                    phaseOneResult = new BookingPaymentResponse
                    {
                        Success = true,
                        Message = "Payment already processed",
                        PaymentId = payment.PaymentId,
                        BookingId = booking.BookingId
                    };
                    return;
                }

                // ── Fresh callback — safe to mutate dbTxn now ─────────────────
                currentStep = "Mutating and saving PaymentTransaction";
                _logger.LogInformation("╠══ STEP: {Step}", currentStep);

                if (!string.IsNullOrWhiteSpace(transactionId) && long.TryParse(transactionId, out _))
                    dbTxn.PaymobTransactionId = transactionId;

                if (!string.IsNullOrWhiteSpace(orderId) && long.TryParse(orderId, out _)
                    && string.IsNullOrWhiteSpace(dbTxn.PaymobNumericOrderId))
                    dbTxn.PaymobNumericOrderId = orderId;

                if (!string.IsNullOrWhiteSpace(orderId) && !long.TryParse(orderId, out _)
                    && dbTxn.PaymobOrderId != orderId && orderId != dbTxn.PaymobIntentionId)
                    dbTxn.PaymobOrderId = orderId;

                dbTxn.GatewayStatus = isSuccess ? PaymentGatewayStatus.Success : PaymentGatewayStatus.Failed;
                dbTxn.CallbackProcessedAt = DateTime.UtcNow;
                dbTxn.CompletedAt = isSuccess ? DateTime.UtcNow : null;

                if (isSuccess) dbTxn.CallbackSuccess = "true";
                else           dbTxn.CallbackFailed  = "true";
                dbTxn.CallbackPending = null;

                await _unitOfWork.PaymentTransactions.Update(dbTxn);
                _logger.LogInformation("╠══ OK: PaymentTransaction.Update() called.");

                currentStep = "Loading Student entity";
                _logger.LogInformation("╠══ STEP: {Step} | StudentId={StudentId}", currentStep, booking.StudentId);
                var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
                if (student == null)
                    _logger.LogError("╠══ WARNING: Student not found for StudentId={StudentId}", booking.StudentId);
                else
                    _logger.LogInformation("╠══ OK: Student. Id={StudentId} UserId={UserId}", student.StudentId, student.UserId);

                // ── FAILURE ──────────────────────────────────────────────────
                if (!isSuccess)
                {
                    currentStep = "Marking Payment Failed";
                    _logger.LogInformation("╠══ STEP: {Step}", currentStep);
                    payment.PaymentStatus = PaymentStatus.Failed;
                    payment.TransactionId = transactionId;
                    await _unitOfWork.Payments.Update(payment);
                    booking.BookingStatus = BookingStatus.PendingPayment;
                    booking.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Bookings.Update(booking);
                    phaseOneResult = new BookingPaymentResponse { Success = false, Message = "Payment failed", PaymentId = payment.PaymentId };
                    return;
                }

                // ── SUCCESS ───────────────────────────────────────────────────

                // 4. Mark payment Completed
                currentStep = "Marking Payment Completed";
                _logger.LogInformation("╠══ STEP: {Step} | PaymentId={PaymentId} OldStatus={Old}→New=Completed", currentStep, payment.PaymentId, payment.PaymentStatus);
                payment.PaymentStatus = PaymentStatus.Completed;
                payment.CompletedAt = DateTime.UtcNow;
                payment.TransactionId = transactionId;
                await _unitOfWork.Payments.Update(payment);
                _logger.LogInformation("╠══ OK: Payment.Update() called.");

                // 5. Move booking to WaitingForContract
                currentStep = "Updating Booking to WaitingForContract";
                _logger.LogInformation("╠══ STEP: {Step} | BookingId={BookingId} OldStatus={Old}→New=WaitingForContract", currentStep, booking.BookingId, booking.BookingStatus);
                booking.BookingStatus = BookingStatus.WaitingForContract;
                booking.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Bookings.Update(booking);
                _logger.LogInformation("╠══ OK: Booking.Update() called.");

                // 6. Resolve student identity for receipts
                currentStep = "Resolving student identity via UserManager";
                _logger.LogInformation("╠══ STEP: {Step}", currentStep);
                User? studentUser = string.IsNullOrEmpty(student?.UserId)
                    ? null : await _userManager.FindByIdAsync(student.UserId);

                string studentName   = studentUser?.Email ?? studentUser?.UserName
                    ?? student?.User?.Email ?? student?.User?.UserName ?? "Unknown";
                string studentUserId = student?.UserId ?? string.Empty;

                _logger.LogInformation("╠══ OK: Student identity. UserId={UserId} Name={Name}", studentUserId, studentName);

                // 7. Queue student receipt
                currentStep = "Queuing student receipt";
                _logger.LogInformation("╠══ STEP: {Step}", currentStep);
                if (!string.IsNullOrEmpty(studentUserId))
                {
                    var exists = await _unitOfWork.PaymentReceipts.GetAll()
                        .AnyAsync(r => r.PaymentId == payment.PaymentId
                                    && r.IssuedToUserId == studentUserId
                                    && r.Type == ReceiptType.PaymentReceived);
                    _logger.LogInformation("╠══ Student receipt exists={Exists}", exists);
                    if (!exists)
                        receiptRequests.Add(new ReceiptGenerationRequest
                        {
                            PaymentId = payment.PaymentId,
                            EscrowId = null,
                            Type = ReceiptType.PaymentReceived,
                            IssuedToUserId = studentUserId,
                            IssuedToRole = "Student",
                            IssuedToName = studentName,
                            AdditionalData = new Dictionary<string, object>
                            {
                                { "BookingId", booking.BookingId },
                                { "Amount", payment.Amount },
                                { "PaymentDate", DateTime.UtcNow },
                                { "TransactionId", transactionId ?? string.Empty }
                            }
                        });
                }

                // 8. Resolve landlord and queue landlord receipt
                currentStep = "Resolving landlord";
                _logger.LogInformation("╠══ STEP: {Step} | RoomId={RoomId} HousingUnitId={HUId}", currentStep, booking.RoomId, booking.HousingUnitId);
                LandLord? landlord = null;
                if (booking.RoomId.HasValue)
                {
                    var room = await _unitOfWork.Rooms.GetAsync(booking.RoomId.Value);
                    if (room != null)
                    {
                        var hu = await _unitOfWork.HousingUnits.GetAsync(room.HousingUnitId);
                        if (hu != null) landlord = await _unitOfWork.LandLords.GetAsync(hu.LandLordId);
                    }
                }
                else if (booking.HousingUnitId.HasValue)
                {
                    var hu = await _unitOfWork.HousingUnits.GetAsync(booking.HousingUnitId.Value);
                    if (hu != null) landlord = await _unitOfWork.LandLords.GetAsync(hu.LandLordId);
                }
                _logger.LogInformation("╠══ Landlord resolved={Found} LandlordId={LandlordId}", landlord != null, landlord?.LandLordId);

                if (landlord != null && !string.IsNullOrEmpty(landlord.UserId))
                {
                    currentStep = "Queuing landlord receipt";
                    _logger.LogInformation("╠══ STEP: {Step}", currentStep);
                    User? landlordUser = await _userManager.FindByIdAsync(landlord.UserId);
                    string landlordName = landlordUser?.Email ?? landlordUser?.UserName ?? "Unknown";

                    var exists = await _unitOfWork.PaymentReceipts.GetAll()
                        .AnyAsync(r => r.PaymentId == payment.PaymentId
                                    && r.IssuedToUserId == landlord.UserId
                                    && r.Type == ReceiptType.PaymentReceived);
                    _logger.LogInformation("╠══ Landlord receipt exists={Exists}", exists);
                    if (!exists)
                        receiptRequests.Add(new ReceiptGenerationRequest
                        {
                            PaymentId = payment.PaymentId,
                            EscrowId = null,
                            Type = ReceiptType.PaymentReceived,
                            IssuedToUserId = landlord.UserId,
                            IssuedToRole = "LandLord",
                            IssuedToName = landlordName,
                            AdditionalData = new Dictionary<string, object>
                            {
                                { "BookingId", booking.BookingId },
                                { "Amount", payment.Amount },
                                { "PaymentDate", DateTime.UtcNow },
                                { "TransactionId", transactionId ?? string.Empty },
                                { "StudentName", studentName }
                            }
                        });
                }

                // 9. Create escrow (ContractId = null — linked later when admin uploads contract)
                currentStep = "Checking / creating EscrowTransaction";
                _logger.LogInformation("╠══ STEP: {Step}", currentStep);
                var existingEscrow = await _unitOfWork.EscrowTransactions.GetByPaymentIdAsync(payment.PaymentId);
                if (existingEscrow == null && landlord != null && student != null)
                {
                    const decimal FeePercent = 5.0m;
                    await _unitOfWork.EscrowTransactions.Insert(new EscrowTransaction
                    {
                        EscrowId = Guid.NewGuid(),
                        BookingId = booking.BookingId,
                        StudentId = student.StudentId,
                        LandlordId = landlord.LandLordId,
                        PaymentId = payment.PaymentId,
                        ContractId = null,
                        Amount = payment.Amount,
                        Currency = "EGP",
                        Status = EscrowStatus.Holding,
                        TransactionType = "Payment",
                        PaymentReference = transactionId ?? orderId ?? string.Empty,
                        PlatformFee = payment.Amount * (FeePercent / 100),
                        PlatformFeePercentage = FeePercent,
                        CreatedAt = DateTime.UtcNow
                    });
                    _logger.LogInformation("╠══ OK: EscrowTransaction.Insert() called. Will flush on SaveChanges.");
                }
                else if (existingEscrow != null)
                {
                    _logger.LogInformation("╠══ Escrow already exists for payment {PaymentId}: {EscrowId}", payment.PaymentId, existingEscrow.EscrowId);
                }
                else
                {
                    _logger.LogWarning("╠══ Cannot create escrow — landlord={L} student={S}", landlord?.LandLordId, student?.StudentId);
                }

                // 10. Record payment history (only if a valid student user ID is present)
                // ⚠️  ROOT CAUSE WARNING: RecordPaymentEventAsync calls _unitOfWork.SaveChangesAsync()
                //     INTERNALLY. If that SaveChanges throws (FK violation, duplicate key, EF
                //     tracking conflict), the exception propagates up through the operation lambda,
                //     ExecuteInTransactionAsync catches it, rolls back the ENTIRE transaction, and
                //     PaymentStatus stays Pending. Payment history is audit data — it must NOT be
                //     able to roll back the payment itself. The try/catch below isolates that risk.
                currentStep = "Calling RecordPaymentEventAsync";
                _logger.LogInformation("╠══ STEP: {Step} | studentUserId={UserId}", currentStep, studentUserId);

                // Log EF ChangeTracker state before RecordPaymentEventAsync
                _logger.LogInformation("╠══ EF ChangeTracker BEFORE RecordPaymentEventAsync:");
                foreach (var entry in _unitOfWork.GetTrackedEntities())
                    _logger.LogInformation("╠══   TrackedEntity: {Entry}", entry);

                if (!string.IsNullOrEmpty(studentUserId))
                {
                    try
                    {
                        _logger.LogInformation("╠══ ENTERING RecordPaymentEventAsync...");
                        await _paymentHistoryService.RecordPaymentEventAsync(
                            payment.PaymentId, booking.BookingId, null, studentUserId,
                            "PaymentCompleted",
                            $"Payment of {payment.Amount} EGP completed. Booking awaiting contract.",
                            payment.Amount, BookingStatus.PendingPayment.ToString(),
                            BookingStatus.WaitingForContract.ToString(),
                            "System", "System",
                            metadata: new Dictionary<string, object>
                            {
                                { "PaymobOrderId", orderId ?? string.Empty },
                                { "PaymobTransactionId", transactionId ?? string.Empty }
                            });
                        _logger.LogInformation("╠══ EXITED RecordPaymentEventAsync successfully.");
                    }
                    catch (Exception histEx)
                    {
                        var hInner = histEx.InnerException;
                        _logger.LogError(histEx,
                            "╠══ EXCEPTION inside RecordPaymentEventAsync. " +
                            "Message={Msg} | InnerException={Inner} | InnerInner={InnerInner}",
                            histEx.Message, hInner?.Message, hInner?.InnerException?.Message);
                        _logger.LogError("╠══ EF ChangeTracker AFTER RecordPaymentEventAsync failure:");
                        foreach (var entry in _unitOfWork.GetTrackedEntities())
                            _logger.LogError("╠══   TrackedEntity: {Entry}", entry);
                        // Do NOT rethrow — payment history is non-critical audit data.
                        // Rethrowing would roll back the whole transaction (steps 4 & 5).
                        _logger.LogWarning("╠══ RecordPaymentEventAsync failure suppressed. Payment/Booking updates are NOT rolled back.");
                    }
                }
                else
                {
                    _logger.LogWarning("╠══ RecordPaymentEventAsync skipped — studentUserId empty. PaymentId={PaymentId}", payment.PaymentId);
                }

                currentStep = "Building phaseOneResult";
                _logger.LogInformation("╠══ STEP: {Step}", currentStep);
                phaseOneResult = new BookingPaymentResponse
                {
                    Success = true,
                    Message = "Payment completed. Receipt issued. Waiting for admin to upload contract.",
                    PaymentId = payment.PaymentId,
                    BookingId = booking.BookingId
                };

                // Log ChangeTracker just before ExecuteInTransactionAsync calls SaveChanges+Commit
                currentStep = "Exiting operation lambda (SaveChanges + CommitAsync will follow)";
                _logger.LogInformation("╠══ STEP: {Step}", currentStep);
                _logger.LogInformation("╠══ EF ChangeTracker BEFORE final SaveChanges:");
                foreach (var entry in _unitOfWork.GetTrackedEntities())
                    _logger.LogInformation("╠══   TrackedEntity: {Entry}", entry);

                _logger.LogInformation("╠══ Phase 1 operation lambda complete. PaymentId={PaymentId} BookingId={BookingId}", payment.PaymentId, booking.BookingId);
            });

            currentStep = "ExecuteInTransactionAsync completed (SaveChanges + Commit succeeded)";
            _logger.LogInformation("╠══ STEP: {Step}", currentStep);
        }
        catch (Exception ex)
        {
            var inner  = ex.InnerException;
            var inner2 = inner?.InnerException;
            var inner3 = inner2?.InnerException;
            _logger.LogError(ex,
                "╠══ EXCEPTION in Phase 1 at step='{Step}' | OrderId={OrderId}\n" +
                "  Message          : {Msg}\n" +
                "  InnerException   : {Inner}\n" +
                "  Inner.Inner      : {Inner2}\n" +
                "  Inner.Inner.Inner: {Inner3}",
                currentStep, orderId, ex.Message,
                inner?.Message, inner2?.Message, inner3?.Message);
            _logger.LogError("╠══ EF ChangeTracker at point of failure:");
            try
            {
                foreach (var entry in _unitOfWork.GetTrackedEntities())
                    _logger.LogError("╠══   TrackedEntity: {Entry}", entry);
            }
            catch (Exception ctEx)
            {
                _logger.LogError(ctEx, "╠══ Could not read ChangeTracker after exception");
            }
            return new BookingPaymentResponse { Success = false, Message = $"Error at step '{currentStep}': {ex.Message}" };
        }

        _logger.LogInformation("╠══ Phase 1 SUCCESS. PaymentId={PaymentId} BookingId={BookingId}", phaseOneResult.PaymentId, phaseOneResult.BookingId);

        // ── Phase 2: generate PDFs + save receipts (outside the DB transaction) ──
        // PDF generation + file I/O cannot be rolled back, so they run after commit.
        // Each receipt uses its own internal SaveChangesAsync which is fine here.
        foreach (var req in receiptRequests)
        {
            try
            {
                await _receiptService.GeneratePaymentReceiptAsync(req);
                _logger.LogInformation("Receipt generated for {Role}: {UserId}", req.IssuedToRole, req.IssuedToUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Non-fatal: receipt generation failed for {Role} {UserId}", req.IssuedToRole, req.IssuedToUserId);
            }
        }

        // ── Phase 3: post-commit notifications (non-fatal) ──────────────────────
        try
        {
            await NotifySafe(phaseOneResult.BookingId.ToString(),
                "Payment completed! Your booking is confirmed. The admin will upload the contract shortly.",
                "PaymentCompleted");
        }
        catch { /* non-fatal */ }

        _logger.LogInformation("╚══ ProcessPaymentCallbackAsync EXIT. PaymentId={PaymentId}", phaseOneResult.PaymentId);
        return phaseOneResult;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Admin retry — re-processes the callback for a completed payment
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<BookingPaymentResponse> CompleteBookingWorkflowAsync(Guid paymentId)
    {
        try
        {
            var payment = await _unitOfWork.Payments.GetAsync(paymentId);
            if (payment == null)
                return new BookingPaymentResponse { Success = false, Message = "Payment not found" };

            var txn = await _unitOfWork.PaymentTransactions.GetByPaymentIdAsync(payment.PaymentId);
            if (txn == null)
                return new BookingPaymentResponse { Success = false, Message = "Payment transaction not found" };

            return await ProcessPaymentCallbackAsync(
                txn.PaymobOrderId,
                txn.PaymobTransactionId ?? txn.PaymobOrderId,
                txn.ClientSecret ?? string.Empty,
                isSuccess: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CompleteBookingWorkflowAsync for PaymentId={PaymentId}", paymentId);
            return new BookingPaymentResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Sync all pending payments by querying Paymob Intention details directly
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<int> SyncPendingPaymentsAsync()
    {
        _logger.LogInformation("SyncPendingPaymentsAsync: Starting pending payments synchronization...");
        
        var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(2);
        var pendingTxns = await _unitOfWork.PaymentTransactions
            .GetAll(asNoTracking: true)
            .Where(t => t.GatewayStatus == Domain.Enums.PaymentGatewayStatus.Pending
                     && t.CreatedAt < cutoff
                     && t.CallbackProcessedAt == null)
            .ToListAsync();

        if (pendingTxns.Count == 0)
        {
            _logger.LogInformation("SyncPendingPaymentsAsync: No pending transactions found for synchronization.");
            return 0;
        }

        _logger.LogInformation("SyncPendingPaymentsAsync: Found {Count} pending transactions to sync.", pendingTxns.Count);
        int syncCount = 0;

        foreach (var txn in pendingTxns)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txn.PaymobIntentionId))
                    continue;

                _logger.LogInformation("SyncPendingPaymentsAsync: Querying Paymob for IntentionId: {IntentionId}", txn.PaymobIntentionId);
                var intentionDetails = await _paymobService.GetIntentionDetailsAsync(txn.PaymobIntentionId);
                if (!intentionDetails.HasValue)
                    continue;

                bool paymobSuccess = false;
                string? paymobTransactionId = null;
                string? paymobOrderId = null;

                if (intentionDetails.Value.TryGetProperty("status", out var statusProp))
                {
                    var status = statusProp.GetString()?.ToUpperInvariant();
                    paymobSuccess = status is "CONFIRMED" or "PROCESSED";
                }

                if (intentionDetails.Value.TryGetProperty("transactions", out var transactionsProp)
                    && transactionsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var t in transactionsProp.EnumerateArray())
                    {
                        bool txnSuccess = false;
                        if (t.TryGetProperty("success", out var succProp))
                            txnSuccess = succProp.ValueKind == JsonValueKind.True
                                      || (succProp.ValueKind == JsonValueKind.String
                                         && succProp.GetString()?.ToLowerInvariant() == "true");

                        if (txnSuccess)
                        {
                            paymobSuccess = true;
                            if (t.TryGetProperty("id", out var idProp))
                                paymobTransactionId = idProp.ValueKind == JsonValueKind.Number
                                    ? idProp.GetInt64().ToString()
                                    : idProp.GetString();
                            if (t.TryGetProperty("order", out var orderProp)
                                && orderProp.TryGetProperty("id", out var orderIdProp))
                                paymobOrderId = orderIdProp.ValueKind == JsonValueKind.Number
                                    ? orderIdProp.GetInt64().ToString()
                                    : orderIdProp.GetString();
                            break;
                        }
                    }
                }

                if (paymobSuccess)
                {
                    _logger.LogInformation("SyncPendingPaymentsAsync: IntentionId {IntentionId} is SUCCESS on Paymob. Syncing...", txn.PaymobIntentionId);
                    
                    var result = await ProcessPaymentCallbackAsync(
                        orderId:       paymobOrderId ?? txn.PaymobOrderId,
                        transactionId: paymobTransactionId ?? txn.PaymobTransactionId ?? string.Empty,
                        clientSecret:  txn.ClientSecret ?? string.Empty,
                        isSuccess:     true,
                        paymentId:     txn.PaymentId.ToString());
                    
                    if (result.Success)
                    {
                        syncCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyncPendingPaymentsAsync: Error syncing transaction {TransactionId}", txn.TransactionId);
            }
        }

        _logger.LogInformation("SyncPendingPaymentsAsync: Synchronization completed. {Count} transactions updated.", syncCount);
        return syncCount;
    }

    // ─────────────────────────────────────────────────────────────────────────
    private async Task NotifySafe(string? userId, string message, string type)
    {
        if (string.IsNullOrEmpty(userId)) return;
        try
        {
            await _notificationService.SendRealTimeNotificationAsync(userId, message, type);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Non-fatal: failed to notify user {UserId}", userId);
        }
    }
}
