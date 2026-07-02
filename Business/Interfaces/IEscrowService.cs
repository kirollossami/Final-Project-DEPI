using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IEscrowService
{
    Task<EscrowResponse> CreateEscrowAsync(Guid paymentId, Guid contractId, decimal platformFeePercentage);
    Task<EscrowResponse?> GetEscrowByIdAsync(Guid escrowId);
    Task<EscrowResponse?> GetEscrowByPaymentIdAsync(Guid paymentId);
    Task<EscrowResponse> ReleaseEscrowAsync(EscrowReleaseRequest request);
    Task<EscrowResponse> RefundEscrowAsync(EscrowRefundRequest request);
    Task<EscrowResponse> ProcessOwnerPayoutAsync(Guid escrowId);
}
