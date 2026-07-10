using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Helpers;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class AdminService : IAdminService
{
    private readonly UserManager<User> _userManager;
    private readonly IStudentRepository _studentRepository;
    private readonly ILandLordRepository _landLordRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ICommissionRecordRepository _commissionRecordRepository;

    public AdminService(
        UserManager<User> userManager,
        IStudentRepository studentRepository,
        ILandLordRepository landLordRepository,
        IBookingRepository bookingRepository,
        ICommissionRecordRepository commissionRecordRepository)
    {
        _userManager = userManager;
        _studentRepository = studentRepository;
        _landLordRepository = landLordRepository;
        _bookingRepository = bookingRepository;
        _commissionRecordRepository = commissionRecordRepository;
    }

    public async Task<AdminUserIndexedResponse> GetAllUsersAsync(AdminUserFilterRequest filter)
    {
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            query = query.Where(u => u.Email != null && u.Email.Contains(filter.SearchTerm));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == filter.IsActive.Value);
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var userResponses = new List<AdminUserResponse>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var student = await _studentRepository.GetAll().FirstOrDefaultAsync(s => s.UserId == user.Id);
            var landlord = await _landLordRepository.GetAll().FirstOrDefaultAsync(l => l.UserId == user.Id);

            if (!string.IsNullOrEmpty(filter.Role) && !roles.Contains(filter.Role))
                continue;

            userResponses.Add(new AdminUserResponse
            {
                UserId = user.Id,
                Email = user.Email,
                Roles = roles.ToArray(),
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                StudentId = student?.StudentId,
                LandLordId = landlord?.LandLordId
            });
        }

        return new AdminUserIndexedResponse
        {
            Records = userResponses,
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
        };
    }

    public async Task<ApiResponse<string>> ToggleUserActiveStatusAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new ApiResponse<string>
            {
                Success = false,
                Message = ErrorMessageHelper.UserNotFound
            };
        }

        if (user.IsActive)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Student"))
            {
                var student = await _studentRepository.GetAll().FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (student != null)
                {
                    var activeBookingIds = await _bookingRepository.GetAll()
                        .Where(b => b.StudentId == student.StudentId &&
                                    (b.BookingStatus == BookingStatus.Approved ||
                                     b.BookingStatus == BookingStatus.Approved))
                        .Select(b => b.BookingId)
                        .ToListAsync();

                    if (activeBookingIds.Count > 0)
                    {
                        var ids = string.Join(", ", activeBookingIds);
                        return new ApiResponse<string>
                        {
                            Success = false,
                            Message = string.Format(ErrorMessageHelper.UserHasActiveBookings, ids)
                        };
                    }
                }
            }
            else if (roles.Contains("LandLord"))
            {
                var landlord = await _landLordRepository.GetAll().FirstOrDefaultAsync(l => l.UserId == user.Id);
                if (landlord != null)
                {
                    var activeBookingIds = await _bookingRepository.GetAll()
                        .Where(b => b.Room != null && b.Room.HousingUnit != null &&
                                    b.Room.HousingUnit.LandLordId == landlord.LandLordId &&
                                    (b.BookingStatus == BookingStatus.Approved ||
                                     b.BookingStatus == BookingStatus.Approved))
                        .Select(b => b.BookingId)
                        .ToListAsync();

                    if (activeBookingIds.Count > 0)
                    {
                        var ids = string.Join(", ", activeBookingIds);
                        return new ApiResponse<string>
                        {
                            Success = false,
                            Message = string.Format(ErrorMessageHelper.UserHasActiveBookings, ids)
                        };
                    }
                }
            }
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        return new ApiResponse<string>
        {
            Success = true,
            Message = user.IsActive ? "User activated successfully." : "User deactivated successfully."
        };
    }

    public async Task<GenericIndexedResponse<StudentResponse>> GetPendingVerificationsAsync(int pageNumber, int pageSize)
    {
        var query = _studentRepository.GetAll()
            .Include(s => s.User)
            .Where(s => s.UniversityVerificationStatus == UniversityVerificationStatus.Pending);

        var totalCount = await query.CountAsync();
        var students = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new GenericIndexedResponse<StudentResponse>
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
                NationalId = s.NationalId,
                FacultyName = s.FacultyName,
                UniversityName = s.UniversityName,
                UniversityEmail = s.UniversityEmail,
                UniversityVerificationStatus = s.UniversityVerificationStatus
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        };
    }

    public async Task<StudentResponse?> ReviewUniversityVerificationAsync(Guid studentId, UniversityVerificationStatus newStatus)
    {
        if (newStatus != UniversityVerificationStatus.Approved && newStatus != UniversityVerificationStatus.Rejected)
        {
            return null;
        }

        var student = await _studentRepository.GetAll()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StudentId == studentId);

        if (student == null) return null;

        if (student.UniversityVerificationStatus != UniversityVerificationStatus.Pending)
        {
            return null;
        }

        student.UniversityVerificationStatus = newStatus;
        await _studentRepository.Update(student);
        await _studentRepository.CommitAsync();

        return new StudentResponse
        {
            StudentId = student.StudentId,
            UserId = student.UserId,
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            Address = student.Address,
            City = student.City,
            PreferredArea = student.PreferredArea,
            NationalId = student.NationalId,
            FacultyName = student.FacultyName,
            UniversityName = student.UniversityName,
            UniversityEmail = student.UniversityEmail,
            UniversityVerificationStatus = student.UniversityVerificationStatus
        };
    }

    public async Task<CommissionReportResponse> GetCommissionReportAsync(DateTime? from, DateTime? to)
    {
        var query = _commissionRecordRepository.GetAll().AsQueryable();

        if (from.HasValue)
            query = query.Where(cr => cr.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(cr => cr.CreatedAt <= to.Value);

        var records = await query.ToListAsync();

        return new CommissionReportResponse
        {
            TotalRevenue = records.Sum(r => r.Amount),
            TotalBookings = records.Count,
            AverageCommission = records.Count > 0 ? records.Average(r => r.Amount) : 0,
            FromDate = from,
            ToDate = to,
            Records = records.Select(r => new CommissionRecordResponse
            {
                CommissionRecordId = r.CommissionRecordId,
                BookingId = r.BookingId,
                Rate = r.Rate,
                Amount = r.Amount,
                CreatedAt = r.CreatedAt
            }).ToList()
        };
    }

    public async Task<ApiResponse<string>> UpdateLandlordVerificationStatusAsync(Guid landlordId, string status)
    {
        var validStatuses = new[] { "Approved", "Rejected", "Pending" };
        if (!validStatuses.Contains(status))
        {
            return new ApiResponse<string>
            {
                Success = false,
                Message = "Invalid status. Valid statuses are: Approved, Rejected, Pending."
            };
        }

        var landlord = await _landLordRepository.GetAsync(landlordId);
        if (landlord == null)
        {
            return new ApiResponse<string>
            {
                Success = false,
                Message = "Landlord not found."
            };
        }

        if (status == "Approved")
        {
            if (string.IsNullOrEmpty(landlord.NationalIdImageUrl) || string.IsNullOrEmpty(landlord.HousingUnitDocumentationUrl))
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Cannot approve a landlord with missing or incomplete verification documents."
                };
            }
        }

        landlord.VerificationStatus = status;
        landlord.IsVerified = status == "Approved";
        landlord.UpdatedAt = DateTime.UtcNow;
        await _landLordRepository.Update(landlord);
        await _landLordRepository.CommitAsync();

        return new ApiResponse<string>
        {
            Success = true,
            Message = $"Landlord status updated to {status} successfully."
        };
    }

    public async Task<LandLordIndexedResponse> GetPendingLandlordsAsync(int pageNumber, int pageSize)
    {
        var query = _landLordRepository.GetAll()
            .Include(l => l.User)
            .Where(l => !l.IsVerified);

        var totalCount = await query.CountAsync();
        var landlords = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new LandLordIndexedResponse
        {
            Records = landlords.Select(l => new LandLordResponse
            {
                LandLordId = l.LandLordId,
                UserId = l.UserId,
                CompanyName = l.CompanyName,
                NationalId = l.NationalId,
                NationalIdImageUrl = l.NationalIdImageUrl,
                PropertyOwnerShipProof = l.PropertyOwnerShipProof,
                HousingUnitDocumentationUrl = l.HousingUnitDocumentationUrl,
                VerificationStatus = l.VerificationStatus,
                IsVerified = l.IsVerified,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        };
    }
}
