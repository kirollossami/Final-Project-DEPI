using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Domain.Enums;

namespace Business.Interfaces;

public interface IAdminService
{
    Task<AdminUserIndexedResponse> GetAllUsersAsync(AdminUserFilterRequest filter);
    Task<ApiResponse<string>> ToggleUserActiveStatusAsync(string userId);
    Task<GenericIndexedResponse<StudentResponse>> GetPendingVerificationsAsync(int pageNumber, int pageSize);
    Task<StudentResponse?> ReviewUniversityVerificationAsync(Guid studentId, UniversityVerificationStatus newStatus);
    Task<CommissionReportResponse> GetCommissionReportAsync(DateTime? from, DateTime? to);
    Task<ApiResponse<string>> UpdateLandlordVerificationStatusAsync(Guid landlordId, string status);
    Task<LandLordIndexedResponse> GetPendingLandlordsAsync(int pageNumber, int pageSize);
}
