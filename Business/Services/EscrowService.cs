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

    public async Task<EscrowResponse> CreateEscrowAsync(Guid paymentId, Guid contractId, decimal platformFeePercentage)
    {
        try
        {
            var payment = await _unitOfWork.Payments.GetAsync(paymentId);
            if (payment == null)
            {
                throw new ArgumentException("Payment not found");
            }

            var contract = await _unitOfWork.Contracts.GetAsync(contractId);
            if (contract == null)
            {
                throw new ArgumentException("Contract not found");
            }

            var platformFee = payment.Amount * (platformFeePercentage / 100);

            var escrow = new EscrowTransaction
            {
                EscrowId = Guid.NewGuid(),
                PaymentId = paymentId,
                ContractId = contractId,
                HeldAmount = payment.Amount,
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
            {
                throw new ArgumentException("Escrow transaction not found");
            }

            if (escrow.Status != EscrowStatus.Holding)
            {
                throw new InvalidOperationException("Escrow is not in holding status");
            }

            var contract = await _unitOfWork.Contracts.GetAsync(escrow.ContractId);
            if (contract == null || !contract.IsStudentSigned || !contract.IsOwnerSigned)
            {
                throw new InvalidOperationException("Contract must be signed by both parties before release");
            }

            escrow.Status = EscrowStatus.Released;
            escrow.ReleasedAt = DateTime.UtcNow;
            escrow.ReleasedByUserId = request.AdminUserId;
            escrow.ReleaseNotes = request.ReleaseNotes;
            escrow.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.EscrowTransactions.Update(escrow);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Escrow {escrow.EscrowId} released by admin {request.AdminUserId}");

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

            var paymentTransaction = await _unitOfWork.PaymentTransactions.GetByPaymentIdAsync(escrow.PaymentId);
            if (paymentTransaction == null || string.IsNullOrEmpty(paymentTransaction.PaymobTransactionId))
            {
                throw new InvalidOperationException("Original payment transaction not found");
            }

            // Process refund through Paymob
            var refundSuccess = await _paymobService.RefundTransactionAsync(
                paymentTransaction.PaymobTransactionId,
                escrow.HeldAmount);

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
            {
                throw new ArgumentException("Escrow transaction not found");
            }

            if (escrow.Status != EscrowStatus.Released)
            {
                throw new InvalidOperationException("Escrow must be released before owner payout");
            }

            var contract = await _unitOfWork.Contracts.GetAsync(escrow.ContractId);
            var booking = await _unitOfWork.Bookings.GetAsync(contract.BookingId);

            // Calculate owner payout (held amount minus platform fee)
            var ownerPayoutAmount = escrow.HeldAmount - escrow.PlatformFee;

            // In a real implementation, this would integrate with Paymob's payout API
            // For now, we'll simulate the payout
            var payoutTransactionId = $"PAYOUT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            escrow.OwnerPayoutAmount = ownerPayoutAmount;
            escrow.OwnerPayoutTransactionId = payoutTransactionId;
            escrow.OwnerPayoutAt = DateTime.UtcNow;
            escrow.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.EscrowTransactions.Update(escrow);
            await _unitOfWork.SaveChangesAsync();

            // Update booking status to SuccessfullyConfirmed
            booking.BookingStatus = BookingStatus.SuccessfullyConfirmed;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.Update(booking);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Owner payout processed for escrow {escrow.EscrowId}: {ownerPayoutAmount} EGP");

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
            PaymentId = escrow.PaymentId,
            ContractId = escrow.ContractId,
            HeldAmount = escrow.HeldAmount,
            Currency = escrow.Currency,
            Status = escrow.Status,
            PlatformFee = escrow.PlatformFee,
            CreatedAt = escrow.CreatedAt,
            ReleasedAt = escrow.ReleasedAt,
            ReleaseTransactionId = escrow.ReleaseTransactionId
        };
    }
}
