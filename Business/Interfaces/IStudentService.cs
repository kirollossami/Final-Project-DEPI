using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IStudentService
{
    Task<StudentResponse?> GetStudentByIdAsync(Guid studentId);
    Task<StudentIndexedResponse> GetStudentsAsync(StudentFilterRequest filter);
    Task<StudentResponse?> CreateStudentAsync(StudentRegisterRequest request);
    Task<StudentResponse?> UpdateStudentAsync(StudentUpdateRequest request);
    Task<bool> DeleteStudentAsync(Guid studentId);
    Task<bool> ChangePasswordAsync(ChangePasswordRequest request);
}
