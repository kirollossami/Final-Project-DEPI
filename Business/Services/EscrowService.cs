using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;

namespace Business.Services;

public class EscrowService : IEscrowService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymobService _paymobService;
    private readonly ILogger<EscrowService> _logger;

    public EscrowService(
        IUnitOfWork unitOfWork,
        IPaymobService paymobService,
        ILogger<EscrowService> logger)
    {
        _unitOfWork = unitOfWork;
        _paymobService = paymobService;
        _logger = logger;
    }

    public async Task<EscrowResponse> CreateEscrowAsync(Guid paymentId, Guid? contractId, decimal platformFeePercentage)
    {
        try
        {
            var payment = await _unitOfWork.Payments.GetAsync(paymentId);
            if (payment == null)
            {
                throw new ArgumentException("Payment not found");
            }

            // Load booking to get student and landlord IDs
            var booking = await _unitOfWork.Bookings.GetAsync(payment.BookingId);
            if (booking == null)
            {
                throw new ArgumentException("Booking not found");
            }

            // Load student
            var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
            if (student == null)
            {
                throw new ArgumentException("Student not found");
            }

            // Load landlord
            LandLord? landlord = null;
            if (booking.RoomId.HasValue)
            {
                var room = await _unitOfWork.Rooms.GetAsync(booking.RoomId.Value);
                if (room != null)
                {
                    var housingUnit = await _unitOfWork.HousingUnits.GetAsync(room.HousingUnitId);
                    if (housingUnit != null)
                        landlord = await _unitOfWork.LandLords.GetAsync(housingUnit.LandLordId);
                }
            }
            else if (booking.HousingUnitId.HasValue)
            {
                var housingUnit = await _unitOfWork.HousingUnits.GetAsync(booking.HousingUnitId.Value);
                if (housingUnit != null)
                    landlord = await _unitOfWork.LandLords.GetAsync(housingUnit.LandLordId);
            }

            if (landlord == null)
            {
                throw new ArgumentException("Landlord not found");
            }

            // Contract is now optional - escrow can be created independently
            if (contractId.HasValue && contractId.Value != Guid.Empty)
            {
                var contract = await _unitOfWork.Contracts.GetAsync(contractId.Value);
                if (contract == null)
                {
                    throw new ArgumentException("Contract not found");
                }
            }

            var platformFee = payment.Amount * (platformFeePercentage / 100);

            var escrow = new EscrowTransaction
            {
                EscrowId = Guid.NewGuid(),
                BookingId = booking.BookingId,
                StudentId = student.StudentId,
                LandlordId = landlord.LandLordId,
                PaymentId = paymentId,
                ContractId = (contractId.HasValue && contractId.Value != Guid.Empty) ? contractId : null,
                Amount = payment.Amount,
                Currency = "EGP",
                Status = EscrowStatus.Holding,
                PlatformFee = platformFee,
                PlatformFeePercentage = platformFeePercentage,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.EscrowTransactions.Insert(escrow);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Escrow created for payment {paymentId} with amount {payment.Amount}");

            return MapToResponse(escrow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating escrow");
            throw;
        }
    }

    public async Task<EscrowResponse?> GetEscrowByIdAsync(Guid escrowId)
    {
        var escrow = await _unitOfWork.EscrowTransactions.GetAsync(escrowId);
        return escrow == null ? null : MapToResponse(escrow);
    }

    public async Task<EscrowResponse?> GetEscrowByPaymentIdAsync(Guid paymentId)
    {
        var escrow = await _unitOfWork.EscrowTransactions.GetByPaymentIdAsync(paymentId);
        return escrow == null ? null : MapToResponse(escrow);
    }

    public async Task<EscrowResponse> ReleaseEscrowAsync(EscrowReleaseRequest request)
    {
        try
        {
            var escrow = await _unitOfWork.EscrowTransactions.GetAsync(request.EscrowId);
            if (escrow == null)
                throw new ArgumentException("Escrow transaction not found");

            if (escrow.Status != EscrowStatus.Holding)
                throw new InvalidOperationException("Escrow is not in holding status");

            // ContractId may be null for escrows created before contract upload
            if (!escrow.ContractId.HasValue || escrow.ContractId == Guid.Empty)
                throw new InvalidOperationException("Escrow has no linked contract. Link contract before releasing.");

            var contract = await _unitOfWork.Contracts.GetAsync(escrow.ContractId.Value);
            if (contract == null)
                throw new InvalidOperationException("Contract linked to this escrow was not found.");

            if (!contract.IsStudentSigned || !contract.IsLandlordSigned)
                throw new InvalidOperationException("Contract must be signed by both parties before release");

            escrow.Status = EscrowStatus.Released;
            escrow.ReleasedAt = DateTime.UtcNow;
            escrow.ReleasedByUserId = request.AdminUserId;
            escrow.ReleaseNotes = request.ReleaseNotes;
            escrow.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.EscrowTransactions.Update(escrow);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Escrow {EscrowId} released by admin {AdminUserId}", escrow.EscrowId, request.AdminUserId);

            return MapToResponse(escrow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing escrow");
            throw;
        }
    }

    public async Task<EscrowResponse> RefundEscrowAsync(EscrowRefundRequest request)
    {
        try
        {
            var escrow = await _unitOfWork.EscrowTransactions.GetAsync(request.EscrowId);
            if (escrow == null)
            {
                throw new ArgumentException("Escrow transaction not found");
            }

            if (escrow.Status != EscrowStatus.Holding)
            {
                throw new InvalidOperationException("Escrow is not in holding status");
            }

            var paymentTransaction = await _unitOfWork.PaymentTransactions.GetByPaymentIdAsync(escrow.PaymentId ?? Guid.Empty);
            if (paymentTransaction == null || string.IsNullOrEmpty(paymentTransaction.PaymobTransactionId))
            {
                throw new InvalidOperationException("Original payment transaction not found");
            }

            // Process refund through Paymob
            var refundSuccess = await _paymobService.RefundTransactionAsync(
                paymentTransaction.PaymobTransactionId,
                escrow.Amount);

            if (!refundSuccess)
            {
                throw new InvalidOperationException("Refund processing failed");
            }

            escrow.Status = EscrowStatus.Refunded;
            escrow.RefundedAt = DateTime.UtcNow;
            escrow.RefundTransactionId = paymentTransaction.PaymobTransactionId;
            escrow.RefundReason = request.RefundReason;
            escrow.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.EscrowTransactions.Update(escrow);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Escrow {escrow.EscrowId} refunded by admin {request.AdminUserId}");

            return MapToResponse(escrow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding escrow");
            throw;
        }
    }

    public async Task<EscrowResponse> ProcessOwnerPayoutAsync(Guid escrowId)
    {
        try
        {
            var escrow = await _unitOfWork.EscrowTransactions.GetAsync(escrowId);
            if (escrow == null)
                throw new ArgumentException("Escrow transaction not found");

            if (escrow.Status != EscrowStatus.Released)
                throw new InvalidOperationException("Escrow must be released before owner payout");

            if (!escrow.ContractId.HasValue || escrow.ContractId == Guid.Empty)
                throw new InvalidOperationException("Escrow has no linked contract");

            var contract = await _unitOfWork.Contracts.GetAsync(escrow.ContractId.Value);
            if (contract == null)
                throw new InvalidOperationException("Contract not found for this escrow");

            var booking = await _unitOfWork.Bookings.GetAsync(contract.BookingId);
            if (booking == null)
                throw new InvalidOperationException("Booking not found");

            var ownerPayoutAmount = escrow.Amount - escrow.PlatformFee;
            var payoutTransactionId = $"PAYOUT-{Guid.NewGuid().ToString()[..8].ToUpper()}";

            escrow.LandlordPayoutAmount = ownerPayoutAmount;
            escrow.LandlordPayoutTransactionId = payoutTransactionId;
            escrow.LandlordPayoutAt = DateTime.UtcNow;
            escrow.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.EscrowTransactions.Update(escrow);

            // Only set Approved if not already Approved (AdminApprovalService may have already done this)
            if (booking.BookingStatus != BookingStatus.Approved)
            {
                booking.BookingStatus = BookingStatus.Approved;
                booking.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Bookings.Update(booking);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Owner payout processed for escrow {EscrowId}: {Amount} EGP", escrow.EscrowId, ownerPayoutAmount);

            return MapToResponse(escrow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing owner payout");
            throw;
        }
    }

    private EscrowResponse MapToResponse(EscrowTransaction escrow)
    {
        return new EscrowResponse
        {
            EscrowId = escrow.EscrowId,
            PaymentId = escrow.PaymentId ?? Guid.Empty,
            ContractId = escrow.ContractId ?? Guid.Empty,
            Amount = escrow.Amount,
            Currency = escrow.Currency,
            Status = escrow.Status,
            PlatformFee = escrow.PlatformFee,
            CreatedAt = escrow.CreatedAt,
            ReleasedAt = escrow.ReleasedAt,
            ReleaseTransactionId = escrow.ReleaseTransactionId
        };
    }
}
