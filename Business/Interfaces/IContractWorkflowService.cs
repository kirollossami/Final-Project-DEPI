namespace Business.Interfaces
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// DEPRECATED: This service is no longer used.
    /// The contract workflow has been separated into independent flows:
    /// - Payment flow: BookingPaymentService (receipts + admin balance transfer)
    /// - Contract flow: ContractService (manual upload + signing)
    /// - Escrow flow: EscrowService (created when contract uploaded)
    /// - Approval flow: BookingApprovalService (admin decision + balance transfers)
    /// </summary>
    public interface IContractWorkflowService
    {
        /// <summary>
        /// DEPRECATED: Do not use this method.
        /// Use ContractService.UploadContractAsync for manual contract upload.
        /// </summary>
        [Obsolete("Use ContractService.UploadContractAsync instead")]
        Task StartWorkflowAsync(Guid bookingId, Guid paymentId);
    }
}
