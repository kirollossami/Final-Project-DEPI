using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IContractService
{
    Task<ContractResponse> GenerateContractAsync(ContractGenerationRequest request);
    Task<ContractResponse> SignContractAsync(ContractSignatureRequest request);
    Task<ContractResponse?> GetContractByIdAsync(Guid contractId);
    Task<ContractResponse?> GetContractByBookingIdAsync(Guid bookingId);
    Task<byte[]> GenerateContractPdfAsync(Guid contractId);
}
