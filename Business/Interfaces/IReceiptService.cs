using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IReceiptService
{
    /// <summary>
    /// Generate a payment receipt that does NOT require an escrow transaction.
    /// Used immediately after payment completion (student receipt).
    /// </summary>
    Task<ReceiptResponse> GeneratePaymentReceiptAsync(ReceiptGenerationRequest request);

    /// <summary>
    /// Generate a receipt that IS linked to an existing escrow transaction.
    /// Used for EscrowHeld, EscrowReleased, OwnerPayout, RefundIssued receipts.
    /// </summary>
    Task<ReceiptResponse> GenerateEscrowReceiptAsync(ReceiptGenerationRequest request);

    Task<ReceiptResponse?> GetReceiptByIdAsync(Guid receiptId);
    Task<IEnumerable<ReceiptResponse>> GetReceiptsByUserIdAsync(string userId);
    Task<IEnumerable<ReceiptResponse>> GetReceiptsByPaymentIdAsync(Guid paymentId);
    Task<byte[]> GenerateReceiptPdfAsync(Guid receiptId);
}
