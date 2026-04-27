using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IStudentService
{
    Task<StudentResponse?> GetStudentByIdAsync(Guid studentId);
    Task<StudentResponse?> GetStudentByUserIdAsync(string userId);
    Task<StudentIndexedResponse> GetStudentsAsync(StudentFilterRequest filter);
    Task<StudentResponse?> CreateStudentAsync(StudentRegisterRequest request);
    Task<StudentResponse?> UpdateStudentAsync(StudentUpdateRequest request);
    Task<bool> DeleteStudentAsync(Guid studentId);
    Task<bool> ChangePasswordAsync(ChangePasswordRequest request);
    Task<bool> DeactivateStudentAsync(Guid studentId);
    Task<bool> ReactivateStudentAsync(Guid studentId);
    Task<bool> ValidateNationalIdAsync(string nationalId);
}
