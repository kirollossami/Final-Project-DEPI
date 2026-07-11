using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IBookingApprovalService
{
    /// <summary>
    /// Admin approves the booking after both parties have signed the contract.
    /// Transfers funds from admin balance to landlord balance and releases escrow.
    /// </summary>
    Task<BookingApprovalResponse> ApproveBookingAsync(Guid bookingId, string adminUserId, string? adminNotes = null);

    /// <summary>
    /// Admin rejects the booking after both parties have signed the contract.
    /// Refunds funds from admin balance to student balance and refunds escrow.
    /// </summary>
    Task<BookingApprovalResponse> RejectBookingAsync(Guid bookingId, string adminUserId, string rejectionReason);
}
