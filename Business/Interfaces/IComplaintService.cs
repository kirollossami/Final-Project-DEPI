using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IComplaintService
{
    Task<ComplaintResponse?> GetComplaintByIdAsync(Guid complaintId);
    Task<ComplaintIndexedResponse> GetComplaintsAsync(ComplaintFilterRequest filter);
    Task<ComplaintResponse?> CreateComplaintAsync(ComplaintCreateRequest request);
    Task<ComplaintResponse?> UpdateComplaintAsync(ComplaintUpdateRequest request);
    Task<bool> DeleteComplaintAsync(Guid complaintId);
}
