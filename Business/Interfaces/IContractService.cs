using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IContractService
{
    /// <summary>
    /// Admin manually uploads a contract PDF file for a booking.
    /// Stores the file and creates the Contract record.
    /// </summary>
    Task<ContractResponse> UploadContractAsync(Guid bookingId, Stream pdfStream, string fileName);

    /// <summary>
    /// Record a signature (student or landlord) by storing the signed PDF URL.
    /// Advances booking status automatically when both parties have signed.
    /// </summary>
    Task<ContractResponse> SignContractAsync(ContractSignatureRequest request);

    Task<ContractResponse?> GetContractByIdAsync(Guid contractId);
    Task<ContractResponse?> GetContractByBookingIdAsync(Guid bookingId);

    /// <summary>
    /// Download the stored contract PDF bytes by contract ID.
    /// Returns the original uploaded file or the signed version if available.
    /// </summary>
    Task<byte[]> GetContractPdfAsync(Guid contractId);
}
