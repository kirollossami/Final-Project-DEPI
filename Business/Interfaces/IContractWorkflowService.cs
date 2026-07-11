namespace Business.Interfaces
{
    using System;
    using System.Threading.Tasks;

    public interface IContractWorkflowService
    {
        /// <summary>
        /// Starts the end‑to‑end contract workflow after a successful payment.
        /// It generates the contract PDF, creates an escrow transaction, records receipts, and sends notifications.
        /// </summary>
        Task StartWorkflowAsync(Guid bookingId, Guid paymentId);
    }
}
