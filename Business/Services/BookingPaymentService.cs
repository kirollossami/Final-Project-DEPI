using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;

namespace Business.Services;

public class BookingPaymentService : IBookingPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymobService _paymobService;
    private readonly IContractService _contractService;
    private readonly IEscrowService _escrowService;
    private readonly IReceiptService _receiptService;
    private readonly INotificationService _notificationService;
    private readonly IPaymentHistoryService _paymentHistoryService;
    private readonly IContractWorkflowService _contractWorkflowService;
private readonly ILogger<BookingPaymentService> _logger;
    private const decimal PlatformFeePercentage = 5.0m; // 5% platform fee

    public BookingPaymentService(
        IUnitOfWork unitOfWork,
        IPaymobService paymobService,
        IContractService contractService,
        IEscrowService escrowService,
        IReceiptService receiptService,
        INotificationService notificationService,
        IPaymentHistoryService paymentHistoryService,
        IContractWorkflowService contractWorkflowService,
        ILogger<BookingPaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _paymobService = paymobService;
        _contractService = contractService;
        _escrowService = escrowService;
        _receiptService = receiptService;
        _notificationService = notificationService;
        _paymentHistoryService = paymentHistoryService;
        _logger = logger;
        _contractWorkflowService = contractWorkflowService;
    }

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

                // Allow retrying payment from Pending, PaymentPending, or PaymentProcessing states
                var payableStatuses = new[]
                {
                    BookingStatus.Pending,
                    BookingStatus.PaymentPending,
                    BookingStatus.PaymentProcessing
                };

                if (!payableStatuses.Contains(booking.BookingStatus))
                    return new BookingPaymentResponse { Success = false, Message = $"Booking cannot be paid. Current status: {booking.BookingStatus}" };

                // Guard: Paymob must be configured
                if (string.IsNullOrWhiteSpace(request.CustomerEmail))
                    return new BookingPaymentResponse { Success = false, Message = "Missing required payment details (CustomerEmail)" };

                // Step 2: Update booking status to PaymentPending
                booking.BookingStatus = BookingStatus.PaymentPending;
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

                // Step 6: Update booking status to PaymentProcessing
                booking.BookingStatus = BookingStatus.PaymentProcessing;
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
                                BookingStatus.Pending.ToString(),
                                BookingStatus.PaymentProcessing.ToString(),
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

    public async Task<BookingPaymentResponse> ProcessPaymentCallbackAsync(string orderId, string transactionId, bool isSuccess)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            // Step 1: Find payment transaction by Paymob order ID
            var paymentTransaction = await _unitOfWork.PaymentTransactions.GetByPaymobOrderIdAsync(orderId);
            if (paymentTransaction == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BookingPaymentResponse
                {
                    Success = false,
                    Message = "Payment transaction not found"
                };
            }

            // Step 2: Update payment transaction
            paymentTransaction.PaymobTransactionId = transactionId;
            paymentTransaction.GatewayStatus = isSuccess ? PaymentGatewayStatus.Success : PaymentGatewayStatus.Failed;
            paymentTransaction.CallbackProcessedAt = DateTime.UtcNow;
            paymentTransaction.CompletedAt = isSuccess ? DateTime.UtcNow : null;

            await _unitOfWork.PaymentTransactions.Update(paymentTransaction);

            // Step 3: Update payment status
            var payment = await _unitOfWork.Payments.GetAsync(paymentTransaction.PaymentId);
            if (payment == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BookingPaymentResponse
                {
                    Success = false,
                    Message = "Payment record not found"
                };
            }

            payment.PaymentStatus = isSuccess ? PaymentStatus.Completed : PaymentStatus.Failed;
            payment.CompletedAt = isSuccess ? DateTime.UtcNow : null;
            payment.TransactionId = transactionId;

            await _unitOfWork.Payments.Update(payment);

            // Step 4: Update booking status
            var booking = await _unitOfWork.Bookings.GetAsync(payment.BookingId);
            if (booking == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BookingPaymentResponse
                {
                    Success = false,
                    Message = "Booking not found"
                };
            }

            var previousStatus = booking.BookingStatus.ToString();
            var student = await _unitOfWork.Students.GetAsync(booking.StudentId);

            if (isSuccess)
            {
                // Do NOT auto-approve or generate contract. Keep booking as Pending and mark payment as Paid.
                payment.PaymentStatus = PaymentStatus.Completed;
                payment.CompletedAt = DateTime.UtcNow;
                await _unitOfWork.Payments.Update(payment);

                // Keep booking status as Pending (waiting for admin review). Update timestamp.
                booking.BookingStatus = BookingStatus.Pending;
                booking.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Bookings.Update(booking);

                // Record payment history - Payment Completed
                await _paymentHistoryService.RecordPaymentEventAsync(
                    payment.PaymentId,
                    booking.BookingId,
                    null,
                    student?.UserId ?? string.Empty,
                    "PaymentCompleted",
                    $"Payment completed successfully. Amount: {payment.Amount} EGP. Awaiting admin approval.",
                    payment.Amount,
                    previousStatus,
                    BookingStatus.Pending.ToString(),
                    "System",
                    "System",
                    metadata: new Dictionary<string, object>
                    {
                        { "PaymobOrderId", orderId },
                        { "PaymobTransactionId", transactionId }
                    });

                await _unitOfWork.CommitTransactionAsync();

                // Notify admins to review this paid booking
                _ = Task.Run(async () => await _notificationService.SendNotificationToRoleAsync(
                    "Admin",
                    $"Booking {booking.BookingId} has been paid and awaits your approval.",
                    "BookingPaid"));

                _logger.LogInformation($"Payment processed and booking {booking.BookingId} set to Pending awaiting admin approval");

                return new BookingPaymentResponse
                {
                    Success = true,
                    Message = "Payment completed successfully. Awaiting admin approval.",
                    PaymentId = payment.PaymentId
                };
            }
            else
            {
                booking.BookingStatus = BookingStatus.PaymentPending;
                booking.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Bookings.Update(booking);

                await _unitOfWork.SaveChangesAsync();

                // Record payment history - Payment Failed
                await _paymentHistoryService.RecordPaymentEventAsync(
                    payment.PaymentId,
                    booking.BookingId,
                    null,
                    student?.UserId ?? string.Empty,
                    "PaymentFailed",
                    $"Payment failed or was cancelled",
                    payment.Amount,
                    previousStatus,
                    BookingStatus.PaymentPending.ToString(),
                    "System",
                    "System",
                    metadata: new Dictionary<string, object>
                    {
                        { "PaymobOrderId", orderId },
                        { "PaymobTransactionId", transactionId }
                    });

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogWarning($"Payment failed for booking {booking.BookingId}");

                return new BookingPaymentResponse
                {
                    Success = false,
                    Message = "Payment failed",
                    PaymentId = payment.PaymentId
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment callback");
            await _unitOfWork.RollbackTransactionAsync();
            return new BookingPaymentResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
        }
    }

    public async Task<BookingPaymentResponse> CompleteBookingWorkflowAsync(Guid paymentId)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            // Step 1: Get payment and booking details
            var payment = await _unitOfWork.Payments.GetAsync(paymentId);
            if (payment == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BookingPaymentResponse
                {
                    Success = false,
                    Message = "Payment not found"
                };
            }

            var booking = await _unitOfWork.Bookings.GetAsync(payment.BookingId);
            if (booking == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BookingPaymentResponse
                {
                    Success = false,
                    Message = "Booking not found"
                };
            }

            var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
            if (student == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BookingPaymentResponse
                {
                    Success = false,
                    Message = "Student not found"
                };
            }

            // Get owner details
            LandLord? owner = null;
            if (booking.RoomId.HasValue)
            {
                var room = await _unitOfWork.Rooms.GetAsync(booking.RoomId.Value);
                if (room != null)
                {
                    var housingUnit = await _unitOfWork.HousingUnits.GetAsync(room.HousingUnitId);
                    if (housingUnit != null)
                    {
                        owner = await _unitOfWork.LandLords.GetAsync(housingUnit.LandLordId);
                    }
                }
            }
            else if (booking.HousingUnitId.HasValue)
            {
                var housingUnit = await _unitOfWork.HousingUnits.GetAsync(booking.HousingUnitId.Value);
                if (housingUnit != null)
                {
                    owner = await _unitOfWork.LandLords.GetAsync(housingUnit.LandLordId);
                }
            }

            if (owner == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BookingPaymentResponse
                {
                    Success = false,
                    Message = "Owner not found"
                };
            }

            // Step 2: Generate contract
            var contractRequest = new ContractGenerationRequest
            {
                BookingId = booking.BookingId,
                ReceivingDate = booking.StartDate,
                HandoverDate = booking.EndDate,
                FinalPrice = booking.TotalPrice,
                DurationType = booking.BookingType == BookingType.Monthly ? ContractDurationType.Monthly : ContractDurationType.Yearly,
                DurationValue = CalculateDuration(booking.StartDate, booking.EndDate, booking.BookingType),
                OwnerFullName = owner.User?.UserName ?? "Unknown",
                OwnerNationalId = owner.NationalId,
                StudentFullName = student.User?.UserName ?? "Unknown",
                StudentNationalId = student.NationalId ?? "N/A"
            };

            var contractResponse = await _contractService.GenerateContractAsync(contractRequest);

            if (contractResponse == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BookingPaymentResponse
                {
                    Success = false,
                    Message = "Contract generation failed"
                };
            }

            // Step 3: Update booking with contract info
            var previousStatus = booking.BookingStatus.ToString();
            booking.ContractId = contractResponse.ContractId;
            booking.ContractPdfUrl = contractResponse.GeneratedPdfUrl;
            booking.BookingStatus = BookingStatus.WaitingStudentSignature;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.Update(booking);

            // Step 4: Create escrow transaction
            var escrowResponse = await _escrowService.CreateEscrowAsync(
                payment.PaymentId,
                contractResponse.ContractId,
                PlatformFeePercentage);

            // Step 5: Record payment history - Escrow Created
            await _paymentHistoryService.RecordPaymentEventAsync(
                payment.PaymentId,
                booking.BookingId,
                escrowResponse.EscrowId,
                student.UserId ?? string.Empty,
                "EscrowCreated",
                $"Escrow created to hold payment. Amount: {escrowResponse.HeldAmount} EGP. Platform Fee: {escrowResponse.PlatformFee} EGP",
                escrowResponse.HeldAmount,
                previousStatus,
                BookingStatus.WaitingStudentSignature.ToString(),
                "System",
                "System",
                metadata: new Dictionary<string, object>
                {
                    { "EscrowId", escrowResponse.EscrowId },
                    { "HeldAmount", escrowResponse.HeldAmount },
                    { "PlatformFeePercentage", PlatformFeePercentage }
                });

            // Step 6: Generate receipt for student
            var receiptRequest = new ReceiptGenerationRequest
            {
                PaymentId = payment.PaymentId,
                EscrowId = escrowResponse.EscrowId,
                Type = ReceiptType.PaymentReceived,
                IssuedToUserId = student.UserId ?? string.Empty,
                IssuedToRole = "Student",
                IssuedToName = student.User?.UserName ?? "Unknown",
                AdditionalData = new Dictionary<string, object>
                {
                    { "BookingId", booking.BookingId },
                    { "ContractId", contractResponse.ContractId }
                }
            };

            var receiptResponse = await _receiptService.GenerateReceiptAsync(receiptRequest);

            // Step 7: Generate receipt for escrow hold
            var escrowReceiptRequest = new ReceiptGenerationRequest
            {
                PaymentId = payment.PaymentId,
                EscrowId = escrowResponse.EscrowId,
                Type = ReceiptType.EscrowHeld,
                IssuedToUserId = student.UserId ?? string.Empty,
                IssuedToRole = "Student",
                IssuedToName = student.User?.UserName ?? "Unknown",
                AdditionalData = new Dictionary<string, object>
                {
                    { "HeldAmount", escrowResponse.HeldAmount },
                    { "PlatformFee", escrowResponse.PlatformFee }
                }
            };

            await _receiptService.GenerateReceiptAsync(escrowReceiptRequest);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation($"Booking workflow completed for booking {booking.BookingId}");

            // Send notifications
            await _notificationService.SendRealTimeNotificationAsync(
                student.UserId ?? string.Empty,
                "Payment completed successfully. Please download and sign the contract.",
                "ContractReady");

            await _notificationService.SendRealTimeNotificationAsync(
                owner.UserId ?? string.Empty,
                $"New booking payment received. Contract ID: {contractResponse.ContractNumber}",
                "NewBooking");

            return new BookingPaymentResponse
            {
                Success = true,
                Message = "Booking workflow completed successfully",
                PaymentId = payment.PaymentId,
                ContractId = contractResponse.ContractId,
                ContractPdfUrl = contractResponse.GeneratedPdfUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing booking workflow");
            await _unitOfWork.RollbackTransactionAsync();
            return new BookingPaymentResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
        }
    }

    private int CalculateDuration(DateTime startDate, DateTime endDate, BookingType bookingType)
    {
        var duration = endDate - startDate;
        
        return bookingType switch
        {
            BookingType.Monthly => (int)(duration.TotalDays / 30),
            BookingType.Yearly => (int)(duration.TotalDays / 365),
            _ => (int)duration.TotalDays
        };
    }
}
