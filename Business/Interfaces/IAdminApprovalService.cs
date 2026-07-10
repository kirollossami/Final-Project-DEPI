using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IAdminApprovalService
{
    // ── Contract workflow (admin-manual) ──────────────────────────────────────

    /// <summary>
    /// Admin uploads a contract PDF for a booking that has completed payment.
    /// Creates the Contract record, creates the EscrowTransaction,
    /// and moves the booking to WaitingForSignatures.
    /// Notifies student and landlord that the contract is ready.
    /// </summary>
    Task<AdminContractApprovalResponse> UploadContractAsync(AdminUploadContractRequest request);

    /// <summary>
    /// Admin approves a fully-signed contract.
    /// Releases escrow → transfers (amount - platformFee) to landlord balance.
    /// Generates OwnerPayout receipt for landlord and EscrowReleased receipt for admin.
    /// Moves booking to Approved.
    /// </summary>
    Task<AdminContractApprovalResponse> ApproveContractAsync(AdminContractApprovalRequest request);

    /// <summary>
    /// Admin rejects a contract.
    /// Refunds the full escrow amount from Admin balance back to the student (RefundIssued receipt).
    /// Moves booking to Rejected.
    /// </summary>
    Task<AdminContractApprovalResponse> RejectContractAsync(AdminContractApprovalRequest request);

    // ── Standalone escrow operations ──────────────────────────────────────────
    Task<AdminContractApprovalResponse> ProcessEscrowReleaseAsync(EscrowReleaseRequest request);
    Task<AdminContractApprovalResponse> ProcessEscrowRefundAsync(EscrowRefundRequest request);

    // ── Queries ───────────────────────────────────────────────────────────────
    Task<IEnumerable<ContractResponse>> GetPendingContractsAsync();
    Task<IEnumerable<EscrowResponse>> GetPendingEscrowReleasesAsync();
}
