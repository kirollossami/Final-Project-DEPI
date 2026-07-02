using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IReceiptService
{
    Task<ReceiptResponse> GenerateReceiptAsync(ReceiptGenerationRequest request);
    Task<ReceiptResponse?> GetReceiptByIdAsync(Guid receiptId);
    Task<IEnumerable<ReceiptResponse>> GetReceiptsByUserIdAsync(string userId);
    Task<byte[]> GenerateReceiptPdfAsync(Guid receiptId);
}
