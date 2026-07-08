using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services;

public class ContractWorkflowService : IContractWorkflowService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContractService _contractService;
    private readonly IEscrowService _escrowService;
    private readonly IReceiptService _receiptService;
    private readonly INotificationService _notificationService;
    private readonly IPaymentHistoryService _paymentHistoryService;
    private readonly ILogger<ContractWorkflowService> _logger;
    private const decimal PlatformFeePercentage = 5.0m; // 5% platform fee

    public ContractWorkflowService(
        IUnitOfWork unitOfWork,
        IContractService contractService,
        IEscrowService escrowService,
        IReceiptService receiptService,
        INotificationService notificationService,
        IPaymentHistoryService paymentHistoryService,
        ILogger<ContractWorkflowService> logger)
    {
        _unitOfWork = unitOfWork;
        _contractService = contractService;
        _escrowService = escrowService;
        _receiptService = receiptService;
        _notificationService = notificationService;
        _paymentHistoryService = paymentHistoryService;
        _logger = logger;
    }

    public async Task StartWorkflowAsync(Guid bookingId, Guid paymentId)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            // 1. Load booking, student, owner
            var booking = await _unitOfWork.Bookings.GetAsync(bookingId);
            if (booking == null)
                throw new ArgumentException("Booking not found");

            var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
            if (student == null)
                throw new ArgumentException("Student not found");

            LandLord? owner = null;
            if (booking.RoomId.HasValue)
            {
                var room = await _unitOfWork.Rooms.GetAsync(booking.RoomId.Value);
                if (room != null)
                {
                    var housingUnit = await _unitOfWork.HousingUnits.GetAsync(room.HousingUnitId);
                    if (housingUnit != null)
                        owner = await _unitOfWork.LandLords.GetAsync(housingUnit.LandLordId);
                }
            }
            else if (booking.HousingUnitId.HasValue)
            {
                var housingUnit = await _unitOfWork.HousingUnits.GetAsync(booking.HousingUnitId.Value);
                if (housingUnit != null)
                    owner = await _unitOfWork.LandLords.GetAsync(housingUnit.LandLordId);
            }

            if (owner == null)
                throw new ArgumentException("Owner not found");

            // 2. Generate contract
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
                throw new InvalidOperationException("Contract generation failed");

            // 3. Update booking with contract info and move to awaiting signatures
            var previousStatus = booking.BookingStatus.ToString();
            booking.BookingStatus = BookingStatus.WaitingStudentSignature;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.Update(booking);

            // 4. Create escrow to hold the payment
            var escrowResponse = await _escrowService.CreateEscrowAsync(paymentId, contractResponse.ContractId, PlatformFeePercentage);

            // 5. Record payment history for escrow creation
            await _paymentHistoryService.RecordPaymentEventAsync(
                paymentId,
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

            // 6. Generate receipts for student (payment received) and escrow hold
            var receiptRequest = new ReceiptGenerationRequest
            {
                PaymentId = paymentId,
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
            await _receiptService.GenerateReceiptAsync(receiptRequest);

            var escrowReceiptRequest = new ReceiptGenerationRequest
            {
                PaymentId = paymentId,
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

            // 7. Send notifications to student and owner
            await _notificationService.SendRealTimeNotificationAsync(
                student.UserId ?? string.Empty,
                "Payment completed successfully. Please download and sign the contract.",
                "ContractReady");

            await _notificationService.SendRealTimeNotificationAsync(
                owner.UserId ?? string.Empty,
                $"New booking payment received. Contract ID: {contractResponse.ContractNumber}",
                "NewBooking");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in contract workflow");
            await _unitOfWork.RollbackTransactionAsync();
            throw;
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
