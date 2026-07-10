using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Context;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly UserManager<User> _userManager;
    private readonly IBookingRepository _bookingRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly IComplaintRepository _complaintRepository;
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICommissionRecordRepository _commissionRecordRepository;
    private readonly IContractRepository _contractRepository;
    private readonly IEscrowTransactionRepository _escrowTransactionRepository;
    private readonly IPaymentReceiptRepository _paymentReceiptRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPaymentHistoryRepository _paymentHistoryRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IPaymentTransactionRepository _paymentTransactionRepository;
    private readonly StudentHousingDBContext _context;

    public StudentService(
        IStudentRepository studentRepository,
        IFileStorageService fileStorageService,
        UserManager<User> userManager,
        IBookingRepository bookingRepository,
        IReviewRepository reviewRepository,
        IComplaintRepository complaintRepository,
        IWishlistRepository wishlistRepository,
        IPaymentRepository paymentRepository,
        ICommissionRecordRepository commissionRecordRepository,
        IContractRepository contractRepository,
        IEscrowTransactionRepository escrowTransactionRepository,
        IPaymentReceiptRepository paymentReceiptRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPaymentHistoryRepository paymentHistoryRepository,
        INotificationRepository notificationRepository,
        IPaymentTransactionRepository paymentTransactionRepository,
        StudentHousingDBContext context)
    {
        _studentRepository = studentRepository;
        _fileStorageService = fileStorageService;
        _userManager = userManager;
        _bookingRepository = bookingRepository;
        _reviewRepository = reviewRepository;
        _complaintRepository = complaintRepository;
        _wishlistRepository = wishlistRepository;
        _paymentRepository = paymentRepository;
        _commissionRecordRepository = commissionRecordRepository;
        _contractRepository = contractRepository;
        _escrowTransactionRepository = escrowTransactionRepository;
        _paymentReceiptRepository = paymentReceiptRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _paymentHistoryRepository = paymentHistoryRepository;
        _notificationRepository = notificationRepository;
        _paymentTransactionRepository = paymentTransactionRepository;
        _context = context;
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
            NationalId = student.NationalId,
            FacultyName = student.FacultyName,
            UniversityName = student.UniversityName,
            UniversityEmail = student.UniversityEmail,
            UniversityVerificationStatus = student.UniversityVerificationStatus
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
                NationalId = s.NationalId,
                FacultyName = s.FacultyName,
                UniversityName = s.UniversityName,
                UniversityEmail = s.UniversityEmail,
                UniversityVerificationStatus = s.UniversityVerificationStatus
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
        };
    }

    public async Task<StudentResponse?> CreateStudentAsync(StudentRegisterRequest request)
    {
        // This method should NOT be used directly. Use AuthService.RegisterStudentAsync instead
        // which creates the User first and then the Student with the correct UserId
        throw new InvalidOperationException("Use AuthService.RegisterStudentAsync to create a student with proper User account");
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

        await _studentRepository.Update(student);
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
            NationalId = student.NationalId,
            FacultyName = student.FacultyName,
            UniversityName = student.UniversityName,
            UniversityEmail = student.UniversityEmail,
            UniversityVerificationStatus = student.UniversityVerificationStatus
        };
    }

    public async Task<bool> DeleteStudentAsync(Guid studentId)
    {
        var student = await _context.Students
            .Include(s => s.Bookings)
                .ThenInclude(b => b.Payment)
                    .ThenInclude(p => p.PaymentReceipts)
            .Include(s => s.Bookings)
                .ThenInclude(b => b.CommissionRecord)
            .Include(s => s.Bookings)
                .ThenInclude(b => b.Contract)
            .Include(s => s.Reviews)
            .Include(s => s.Complaints)
            .Include(s => s.Wishlists)
            .FirstOrDefaultAsync(s => s.StudentId == studentId);
        
        if (student == null) return false;

        // Delete related records using context directly for single transaction
        if (student.Bookings != null && student.Bookings.Any())
        {
            foreach (var booking in student.Bookings)
            {
                // Delete payment receipts first
                if (booking.Payment != null && booking.Payment.PaymentReceipts != null)
                {
                    foreach (var receipt in booking.Payment.PaymentReceipts)
                    {
                        _context.PaymentReceipts.Remove(receipt);
                    }
                }

                // Delete payment transactions for this payment
                if (booking.Payment != null)
                {
                    var paymentTransactions = await _context.PaymentTransactions
                        .Where(pt => pt.PaymentId == booking.Payment.PaymentId)
                        .ToListAsync();
                    foreach (var pt in paymentTransactions)
                    {
                        _context.PaymentTransactions.Remove(pt);
                    }
                }

                // Delete escrow transactions related to this booking
                var escrowTransactions = await _context.EscrowTransactions
                    .Where(e => e.BookingId == booking.BookingId)
                    .ToListAsync();
                foreach (var escrow in escrowTransactions)
                {
                    _context.EscrowTransactions.Remove(escrow);
                }

                // Delete booking-related entities
                if (booking.Payment != null)
                {
                    _context.Payments.Remove(booking.Payment);
                }
                if (booking.CommissionRecord != null)
                {
                    _context.CommissionRecords.Remove(booking.CommissionRecord);
                }
                if (booking.Contract != null)
                {
                    _context.Contracts.Remove(booking.Contract);
                }
                // Then delete the booking
                _context.Bookings.Remove(booking);
            }
        }

        if (student.Reviews != null && student.Reviews.Any())
        {
            foreach (var review in student.Reviews)
            {
                _context.Reviews.Remove(review);
            }
        }

        if (student.Complaints != null && student.Complaints.Any())
        {
            foreach (var complaint in student.Complaints)
            {
                _context.Complaints.Remove(complaint);
            }
        }

        if (student.Wishlists != null && student.Wishlists.Any())
        {
            foreach (var wishlist in student.Wishlists)
            {
                _context.Wishlists.Remove(wishlist);
            }
        }

        // Delete escrow transactions that have direct StudentId references
        var directEscrowTransactions = await _context.EscrowTransactions
            .Where(e => e.StudentId == studentId)
            .ToListAsync();
        foreach (var escrow in directEscrowTransactions)
        {
            _context.EscrowTransactions.Remove(escrow);
        }

        // Delete refresh tokens for this user
        if (student.UserId != null)
        {
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == student.UserId)
                .ToListAsync();
            foreach (var token in refreshTokens)
            {
                _context.RefreshTokens.Remove(token);
            }

            // Delete notifications for this user
            var notifications = await _context.Notifications
                .Where(n => n.UserId == student.UserId)
                .ToListAsync();
            foreach (var notification in notifications)
            {
                _context.Notifications.Remove(notification);
            }

            // Delete conversations where this student is the student user
            var conversations = await _context.Conversations
                .Where(c => c.StudentUserId == student.UserId)
                .ToListAsync();
            foreach (var conversation in conversations)
            {
                _context.Conversations.Remove(conversation);
            }
        }

        // Delete the User account if it exists
        if (student.UserId != null)
        {
            var user = await _context.Users.FindAsync(student.UserId);
            if (user != null)
            {
                // Delete Identity-related records first
                var userRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == student.UserId)
                    .ToListAsync();
                foreach (var userRole in userRoles)
                {
                    _context.UserRoles.Remove(userRole);
                }

                var userClaims = await _context.UserClaims
                    .Where(uc => uc.UserId == student.UserId)
                    .ToListAsync();
                foreach (var userClaim in userClaims)
                {
                    _context.UserClaims.Remove(userClaim);
                }

                var userLogins = await _context.UserLogins
                    .Where(ul => ul.UserId == student.UserId)
                    .ToListAsync();
                foreach (var userLogin in userLogins)
                {
                    _context.UserLogins.Remove(userLogin);
                }

                var userTokens = await _context.UserTokens
                    .Where(ut => ut.UserId == student.UserId)
                    .ToListAsync();
                foreach (var userToken in userTokens)
                {
                    _context.UserTokens.Remove(userToken);
                }

                _context.Users.Remove(user);
            }
        }

        _context.Students.Remove(student);

        try
        {
            var changes = await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error deleting student: {ex.Message}", ex);
        }
        return true;
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var student = await _studentRepository.GetAll()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StudentId == request.StudentId);
        if (student == null || student.User == null) return false;

        // Validate passwords match
        if (request.NewPassword != request.ConfirmPassword)
        {
            return false;
        }

        // Validate current password is not empty
        if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
        {
            return false;
        }

        var result = await _userManager.ChangePasswordAsync(student.User, request.CurrentPassword, request.NewPassword);
        return result.Succeeded;
    }

    public async Task<StudentResponse?> GetStudentByUserIdAsync(string userId)
    {
        var student = await _studentRepository.GetAll()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null) return null;

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

    public async Task<bool> DeactivateStudentAsync(Guid studentId)
    {
        var student = await _studentRepository.GetAll()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StudentId == studentId);
        
        if (student == null || student.User == null) return false;

        student.User.IsActive = false;
        await _userManager.UpdateAsync(student.User);

        return true;
    }

    public async Task<bool> ReactivateStudentAsync(Guid studentId)
    {
        var student = await _studentRepository.GetAll()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StudentId == studentId);
        
        if (student == null || student.User == null) return false;

        student.User.IsActive = true;
        await _userManager.UpdateAsync(student.User);

        return true;
    }

    public async Task<bool> ValidateNationalIdAsync(string nationalId)
    {
        var existingStudent = await _studentRepository.GetAll()
            .FirstOrDefaultAsync(s => s.NationalId == nationalId);
        
        return existingStudent == null;
    }

    public async Task<string?> GetUniversityIdCardPathAsync(Guid studentId)
    {
        var student = await _studentRepository.GetAll()
            .FirstOrDefaultAsync(s => s.StudentId == studentId);
        return student?.UniversityIdCardPath;
    }

    public async Task<StudentResponse?> SubmitUniversityVerificationAsync(string userId, SubmitUniversityVerificationRequest request, Stream fileStream, string fileName)
    {
        var student = await _studentRepository.GetAll()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null) return null;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        if (!allowedExtensions.Contains(extension))
            return null;

        var savedPath = await _fileStorageService.SaveFileAsync(
            fileStream,
            $"{student.StudentId}_{Guid.NewGuid()}{extension}",
            "university-ids");

        student.FacultyName = request.FacultyName;
        student.UniversityName = request.UniversityName;
        student.UniversityEmail = request.UniversityEmail;
        student.UniversityIdCardPath = savedPath;
        student.UniversityVerificationStatus = UniversityVerificationStatus.Pending;

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
}
