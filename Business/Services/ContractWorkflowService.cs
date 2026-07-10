using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using System;

namespace Business.Services;

/// <summary>
/// DEPRECATED: This service is no longer used.
/// The contract workflow has been separated into independent flows:
/// - Payment flow: BookingPaymentService (receipts + admin balance transfer)
/// - Contract flow: ContractService (manual upload + signing)
/// - Escrow flow: EscrowService (created when contract uploaded)
/// - Approval flow: BookingApprovalService (admin decision + balance transfers)
/// </summary>
[Obsolete("Use ContractService and BookingApprovalService instead")]
public class ContractWorkflowService : IContractWorkflowService
{
    private readonly ILogger<ContractWorkflowService> _logger;

    public ContractWorkflowService(ILogger<ContractWorkflowService> logger)
    {
        _logger = logger;
    }

    public async Task StartWorkflowAsync(Guid bookingId, Guid paymentId)
    {
        _logger.LogWarning(
            "ContractWorkflowService.StartWorkflowAsync is deprecated and should not be called. " +
            "Use ContractService.UploadContractAsync for manual contract upload instead. " +
            "BookingId: {BookingId}, PaymentId: {PaymentId}",
            bookingId, paymentId);

        throw new InvalidOperationException(
            "ContractWorkflowService.StartWorkflowAsync is deprecated. " +
            "Use ContractService.UploadContractAsync for manual contract upload.");
    }
}
