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
    }

    public async Task<BookingPaymentResponse> InitiateBookingPaymentAsync(BookingPaymentRequest request)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            // Step 1: Get booking details
            var booking = await _unitOfWork.Bookings.GetAsync(request.BookingId);
            if (booking == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BookingPaymentResponse
                {
                    Success = false,
                    Message = "Booking not found"
                };
            }

            if (booking.BookingStatus != BookingStatus.Pending)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BookingPaymentResponse
                {
                    Success = false,
                    Message = $"Booking is not in pending status. Current status: {booking.BookingStatus}"
                };
            }

            // Step 2: Update booking status to PaymentPending
            var previousStatus = booking.BookingStatus.ToString();
            booking.BookingStatus = BookingStatus.PaymentPending;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.Update(booking);

            // Step 3: Create payment record
            var payment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                BookingId = booking.BookingId,
                Amount = booking.TotalPrice,
                PaymentMethod = PaymentMethod.Card, // Default to card, can be extended
                PaymentStatus = PaymentStatus.Pending,
                PaymentDate = DateTime.UtcNow,
                TransactionId = Guid.NewGuid().ToString()
            };

            await _unitOfWork.Payments.Insert(payment);
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
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BookingPaymentResponse
                {
                    Success = false,
                    Message = $"Payment initiation failed: {paymobResponse.Message}"
                };
            }

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

            // Step 7: Record payment history - Payment Initiated
            var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
            await _paymentHistoryService.RecordPaymentEventAsync(
                payment.PaymentId,
                booking.BookingId,
                null,
                student?.UserId ?? string.Empty,
                "PaymentInitiated",
                $"Payment initiated for booking. Amount: {booking.TotalPrice} EGP. Awaiting completion.",
                booking.TotalPrice,
                BookingStatus.Pending.ToString(),
                BookingStatus.PaymentProcessing.ToString(),
                "System",
                "System",
                metadata: new Dictionary<string, object>
                {
                    { "PaymobOrderId", paymobResponse.OrderId ?? "" },
                    { "PaymentMethod", PaymentMethod.Card.ToString() }
                });

            // Step 8: Commit transaction
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation($"Payment initiated successfully for booking {booking.BookingId}. Paymob Order ID: {paymobResponse.OrderId}");

            // Send notification to student
            await _notificationService.SendRealTimeNotificationAsync(
                booking.Student?.UserId ?? string.Empty,
                $"Payment initiated for your booking. Please complete the payment to proceed.",
                "PaymentInitiated");

            return new BookingPaymentResponse
            {
                Success = true,
                Message = "Payment initiated successfully",
                PaymentId = payment.PaymentId,
                PaymentUrl = paymobResponse.PaymentUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating booking payment");
            await _unitOfWork.RollbackTransactionAsync();
            return new BookingPaymentResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
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
                booking.BookingStatus = BookingStatus.ContractGenerationPending;
                booking.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Bookings.Update(booking);

                await _unitOfWork.SaveChangesAsync();

                // Record payment history - Payment Completed
                await _paymentHistoryService.RecordPaymentEventAsync(
                    payment.PaymentId,
                    booking.BookingId,
                    null,
                    student?.UserId ?? string.Empty,
                    "PaymentCompleted",
                    $"Payment completed successfully. Amount: {payment.Amount} EGP",
                    payment.Amount,
                    previousStatus,
                    BookingStatus.ContractGenerationPending.ToString(),
                    "System",
                    "System",
                    metadata: new Dictionary<string, object>
                    {
                        { "PaymobOrderId", orderId },
                        { "PaymobTransactionId", transactionId }
                    });

                await _unitOfWork.CommitTransactionAsync();

                // Trigger contract generation workflow
                await CompleteBookingWorkflowAsync(payment.PaymentId);

                _logger.LogInformation($"Payment callback processed successfully for booking {booking.BookingId}");

                return new BookingPaymentResponse
                {
                    Success = true,
                    Message = "Payment completed successfully",
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
            booking.BookingStatus = BookingStatus.AwaitingStudentSignature;
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
                BookingStatus.AwaitingStudentSignature.ToString(),
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
