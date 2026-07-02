using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IAdminApprovalService
{
    Task<AdminContractApprovalResponse> ApproveContractAsync(AdminContractApprovalRequest request);
    Task<AdminContractApprovalResponse> RejectContractAsync(AdminContractApprovalRequest request);
    Task<AdminContractApprovalResponse> ProcessEscrowReleaseAsync(EscrowReleaseRequest request);
    Task<AdminContractApprovalResponse> ProcessEscrowRefundAsync(EscrowRefundRequest request);
    Task<IEnumerable<ContractResponse>> GetPendingContractsAsync();
    Task<IEnumerable<EscrowResponse>> GetPendingEscrowReleasesAsync();
}
