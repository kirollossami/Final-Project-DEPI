using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly UserManager<User> _userManager;

    public StudentService(IStudentRepository studentRepository, UserManager<User> userManager)
    {
        _studentRepository = studentRepository;
        _userManager = userManager;
    }

    public async Task<StudentResponse?> GetStudentByIdAsync(Guid studentId)
    {
        var student = await _studentRepository.GetAll()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StudentId == studentId);

        if (student == null) return null;

        return new StudentResponse
        {
            StudentId = student.StudentId,
            UserId = student.UserId.ToString(),
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            Address = student.Address,
            City = student.City,
            PreferredArea = student.PreferredArea,
            NationalId = student.NationalId
        };
    }

    public async Task<StudentIndexedResponse> GetStudentsAsync(StudentFilterRequest filter)
    {
        var query = _studentRepository.GetAll().Include(s => s.User).AsQueryable();

        if (!string.IsNullOrEmpty(filter.City))
        {
            query = query.Where(s => s.City == filter.City);
        }

        if (!string.IsNullOrEmpty(filter.PreferredArea))
        {
            query = query.Where(s => s.PreferredArea == filter.PreferredArea);
        }

        var totalCount = await query.CountAsync();
        var students = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new StudentIndexedResponse
        {
            Records = students.Select(s => new StudentResponse
            {
                StudentId = s.StudentId,
                UserId = s.UserId,
                DateOfBirth = s.DateOfBirth,
                Gender = s.Gender,
                Address = s.Address,
                City = s.City,
                PreferredArea = s.PreferredArea,
                NationalId = s.NationalId
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
        };
    }

    public async Task<StudentResponse?> CreateStudentAsync(StudentRegisterRequest request)
    {
        var student = new Domain.Entities.Student
        {
            StudentId = Guid.NewGuid(),
            UserId = Guid.NewGuid().ToString(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Address = request.Address,
            City = request.City,
            PreferredArea = request.PreferredArea,
            NationalId = request.NationalId
        };

        await _studentRepository.Insert(student);
        await _studentRepository.CommitAsync();

        return new StudentResponse
        {
            StudentId = student.StudentId,
            UserId = student.UserId.ToString(),
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            Address = student.Address,
            City = student.City,
            PreferredArea = student.PreferredArea,
            NationalId = student.NationalId
        };
    }

    public async Task<StudentResponse?> UpdateStudentAsync(StudentUpdateRequest request)
    {
        var student = await _studentRepository.GetAll()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StudentId == request.StudentId);
        if (student == null) return null;

        if (request.FullName != null && student.User != null)
        {
            student.User.UserName = request.FullName;
        }

        if (request.DateOfBirth.HasValue)
        {
            student.DateOfBirth = request.DateOfBirth.Value;
        }

        if (request.Gender.HasValue)
        {
            student.Gender = request.Gender.Value;
        }

        if (request.Address != null)
        {
            student.Address = request.Address;
        }

        if (request.City != null)
        {
            student.City = request.City;
        }

        if (request.PreferredArea != null)
        {
            student.PreferredArea = request.PreferredArea;
        }

        if (request.NationalId != null)
        {
            student.NationalId = request.NationalId;
        }

        _studentRepository.Update(student);
        await _studentRepository.CommitAsync();

        return new StudentResponse
        {
            StudentId = student.StudentId,
            UserId = student.UserId.ToString(),
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            Address = student.Address,
            City = student.City,
            PreferredArea = student.PreferredArea,
            NationalId = student.NationalId
        };
    }

    public async Task<bool> DeleteStudentAsync(Guid studentId)
    {
        var student = await _studentRepository.GetAsync(studentId);
        if (student == null) return false;

        _studentRepository.Delete(student);
        await _studentRepository.CommitAsync();

        return true;
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var student = await _studentRepository.GetAll()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StudentId == request.StudentId);
        if (student == null || student.User == null) return false;

        var result = await _userManager.ChangePasswordAsync(student.User, request.CurrentPassword ?? string.Empty, request.NewPassword ?? string.Empty);
        return result.Succeeded;
    }
}
