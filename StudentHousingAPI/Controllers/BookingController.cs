using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingController : BaseController
{
    private readonly IBookingService _bookingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BookingController> _logger;
    private readonly IFileStorageService _fileStorageService;
    private readonly INotificationService _notificationService;

    public BookingController(
        IBookingService bookingService, 
        IUnitOfWork unitOfWork, 
        ILogger<BookingController> logger,
        IFileStorageService fileStorageService,
        INotificationService notificationService)
    {
        _bookingService = bookingService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _fileStorageService = fileStorageService;
        _notificationService = notificationService;
    }

    [HttpGet("GetById/{bookingId}")]
    public async Task<ActionResult<BookingResponse>> GetBookingById(Guid bookingId)
    {
        var booking = await _bookingService.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return NotFound();
        }
        return Ok(booking);
    }

    [HttpGet("GetAll")]
    public async Task<ActionResult<BookingIndexedResponse>> GetBookings([FromQuery] BookingFilterRequest filter)
    {
        var bookings = await _bookingService.GetBookingsAsync(filter);
        return Ok(bookings);
    }

    [HttpPost("Create")]
    public async Task<ActionResult<BookingResponse>> CreateBooking([FromBody] BookingCreateRequest request)
    {
        try
        {
            var booking = await _bookingService.CreateBookingAsync(request);
            if (booking == null)
            {
                return BadRequest("Invalid booking request or booking conflict");
            }
            try
            {
                var student = await _unitOfWork.Students.GetAsync(request.StudentId);
                if (!string.IsNullOrEmpty(student?.UserId))
                    await _notificationService.SendRealTimeNotificationAsync(student.UserId, "Your booking has been created successfully.", NotificationTypes.BookingCreated);
            }
            catch { }
            try
            {
                if (booking.HousingUnitId.HasValue)
                {
                    var unit = await _unitOfWork.HousingUnits.GetAsync(booking.HousingUnitId.Value);
                    if (unit != null)
                    {
                        var landlord = await _unitOfWork.LandLords.GetAsync(unit.LandLordId);
                        if (landlord?.UserId != null)
                            await _notificationService.SendRealTimeNotificationAsync(landlord.UserId, "A new booking has been made for your property.", NotificationTypes.NewBooking);
                    }
                }
            }
            catch { }
            return CreatedAtAction(nameof(GetBookingById), new { bookingId = booking.BookingId }, booking);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("Update")]
    public async Task<ActionResult<BookingResponse>> UpdateBooking([FromBody] BookingUpdateRequest request)
    {
        var booking = await _bookingService.UpdateBookingAsync(request);
        if (booking == null)
        {
            return NotFound();
        }
        return Ok(booking);
    }

    [HttpDelete("Cancel/{bookingId}")]
    public async Task<ActionResult> CancelBooking(Guid bookingId)
    {
        var result = await _bookingService.CancelBookingAsync(bookingId);
        if (!result)
        {
            return NotFound();
        }
        try
        {
            var booking = await _unitOfWork.Bookings.GetAsync(bookingId);
            if (booking != null)
            {
                var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
                if (student?.UserId != null)
                    await _notificationService.SendRealTimeNotificationAsync(student.UserId, "Your booking has been cancelled.", NotificationTypes.BookingCancelled);
            }
        }
        catch { }
        return NoContent();
    }

    [HttpPost("MultiRoom")]
    public async Task<ActionResult<List<BookingResponse?>>> CreateMultiRoomBooking([FromBody] MultiRoomBookingCreateRequest request)
    {
        var result = await _bookingService.CreateMultiRoomBookingAsync(request);
        if (result != null && result.Any())
        {
            try
            {
                var student = await _unitOfWork.Students.GetAsync(request.StudentId);
                if (!string.IsNullOrEmpty(student?.UserId))
                    await _notificationService.SendRealTimeNotificationAsync(student.UserId, "Your multi-room booking has been created.", NotificationTypes.MultiRoomBookingCreated);
            }
            catch { }
        }
        return Ok(result);
    }

    /// <summary>
    /// Get current user's booking history
    /// Students see their bookings, landlords see bookings for their properties
    /// </summary>
    [HttpGet("my-history")]
    public async Task<IActionResult> GetMyBookingHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not identified" });
            }

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var filter = new BookingFilterRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            if (userRole == "Student")
            {
                // Get student ID and filter by student
                var studentId = await GetStudentIdFromUserId(userId);
                if (studentId == null)
                {
                    return BadRequest(new { Message = "Student profile not found" });
                }
                filter.StudentId = studentId;
            }
            else if (userRole == "LandLord")
            {
                // Get landlord's housing units and filter by those
                var landlordId = await GetLandlordIdFromUserId(userId);
                if (landlordId == null)
                {
                    return BadRequest(new { Message = "Landlord profile not found" });
                }
                
                // Get housing units belonging to this landlord
                var housingUnits = await _unitOfWork.HousingUnits.GetAll()
                    .Where(h => h.LandLordId == landlordId)
                    .Select(h => h.HousingUnitId)
                    .ToListAsync();
                
                if (housingUnits.Any())
                {
                    // Filter bookings by housing units - need to handle this differently since filter doesn't support multiple housing units
                    // For now, we'll get all bookings and filter manually
                    var allBookings = await _bookingService.GetBookingsAsync(new BookingFilterRequest { PageNumber = 1, PageSize = 1000 });
                    var landlordBookings = allBookings.Records
                        .Where(b => housingUnits.Contains(b.HousingUnitId ?? Guid.Empty))
                        .ToList();
                    
                    return Ok(new
                    {
                        Success = true,
                        Data = landlordBookings,
                        TotalRecords = landlordBookings.Count,
                        PageIndex = 0,
                        PageSize = landlordBookings.Count,
                        UserRole = userRole
                    });
                }
            }
            else if (userRole == "Admin")
            {
                // Admins can see all bookings
            }

            // Filter by status if provided
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, true, out var bookingStatus))
            {
                filter.BookingStatus = bookingStatus;
            }

            var bookings = await _bookingService.GetBookingsAsync(filter);

            _logger.LogInformation($"User {userId} retrieved {bookings.Records.Count()} bookings from history");

            return Ok(new
            {
                Success = true,
                Data = bookings.Records,
                TotalRecords = bookings.TotalRecords,
                PageIndex = bookings.PageIndex,
                PageSize = bookings.PageSize,
                UserRole = userRole
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking history");
            return BadRequest(new { Message = "Error retrieving booking history", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get booking history with payment receipts for current user
    /// </summary>
    [HttpGet("my-history-with-receipts")]
    public async Task<IActionResult> GetMyBookingHistoryWithReceipts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not identified" });
            }

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var filter = new BookingFilterRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            if (userRole == "Student")
            {
                var studentId = await GetStudentIdFromUserId(userId);
                if (studentId == null)
                {
                    return BadRequest(new { Message = "Student profile not found" });
                }
                filter.StudentId = studentId;
            }
            else if (userRole == "LandLord")
            {
                // Get landlord's housing units and filter by those
                var landlordId = await GetLandlordIdFromUserId(userId);
                if (landlordId == null)
                {
                    return BadRequest(new { Message = "Landlord profile not found" });
                }
                
                // Get housing units belonging to this landlord
                var housingUnits = await _unitOfWork.HousingUnits.GetAll()
                    .Where(h => h.LandLordId == landlordId)
                    .Select(h => h.HousingUnitId)
                    .ToListAsync();
                
                if (housingUnits.Any())
                {
                    // Filter bookings by housing units
                    var allBookings = await _bookingService.GetBookingsAsync(new BookingFilterRequest { PageNumber = 1, PageSize = 1000 });
                    var landlordBookings = allBookings.Records
                        .Where(b => housingUnits.Contains(b.HousingUnitId ?? Guid.Empty))
                        .ToList();
                    
                    // Get receipts for each booking
                    var landlordBookingHistory = new List<object>();
                    foreach (var booking in landlordBookings)
                    {
                        var receipts = await _unitOfWork.PaymentReceipts.GetAll()
                            .Where(r => r.ReceiptData.Contains(booking.BookingId.ToString()))
                            .ToListAsync();

                        landlordBookingHistory.Add(new
                        {
                            booking,
                            receipts = receipts.Select(r => new
                            {
                                r.ReceiptId,
                                r.ReceiptNumber,
                                r.Amount,
                                r.Currency,
                                r.Type,
                                ReceiptPdfUrl = $"/api/Receipt/{r.ReceiptId}/download",
                                r.CreatedAt
                            })
                        });
                    }
                    
                    return Ok(new
                    {
                        Success = true,
                        Data = landlordBookingHistory,
                        TotalRecords = landlordBookings.Count,
                        PageIndex = 0,
                        PageSize = landlordBookings.Count,
                        UserRole = userRole
                    });
                }
            }

            var bookings = await _bookingService.GetBookingsAsync(filter);

            // Get receipts for each booking
            var bookingHistory = new List<object>();
            foreach (var booking in bookings.Records)
            {
                var receipts = await _unitOfWork.PaymentReceipts.GetAll()
                    .Where(r => r.ReceiptData.Contains(booking.BookingId.ToString()))
                    .ToListAsync();

                bookingHistory.Add(new
                {
                    booking,
                    receipts = receipts.Select(r => new
                    {
                        r.ReceiptId,
                        r.ReceiptNumber,
                        r.Amount,
                        r.Currency,
                        r.Type,
                        ReceiptPdfUrl = $"/api/Receipt/{r.ReceiptId}/download",
                        r.CreatedAt
                    })
                });
            }

            return Ok(new
            {
                Success = true,
                Data = bookingHistory,
                TotalRecords = bookings.TotalRecords,
                PageIndex = bookings.PageIndex,
                PageSize = bookings.PageSize,
                UserRole = userRole
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking history with receipts");
            return BadRequest(new { Message = "Error retrieving booking history with receipts", Error = ex.Message });
        }
    }

    /// <summary>
    /// Upload signed contract by student
    /// </summary>
    [HttpPost("{bookingId}/upload-student-signature")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> UploadStudentSignature(Guid bookingId, IFormFile signedContract)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not identified" });

            var booking = await _unitOfWork.Bookings.GetAsync(bookingId);
            if (booking == null)
                return NotFound(new { Message = "Booking not found" });

            // Verify student owns this booking
            var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
            if (student?.UserId != userId)
                return Forbid("You can only sign your own bookings");

            if (booking.BookingStatus != BookingStatus.WaitingForSignatures && 
                booking.BookingStatus != BookingStatus.WaitingForLandlordSignature)
                return BadRequest(new { Message = $"Booking is not ready for student signature. Current status: {booking.BookingStatus}" });

            if (signedContract == null || signedContract.Length == 0)
                return BadRequest(new { Message = "Signed contract file is required" });

            if (!signedContract.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { Message = "Only PDF files are allowed" });

            var contract = await _unitOfWork.Contracts.GetByBookingIdAsync(bookingId);
            if (contract == null)
                return NotFound(new { Message = "Contract not found" });

            // Upload signed contract
            var fileName = $"student_signed_{bookingId}_{Guid.NewGuid()}.pdf";
            var filePath = await _fileStorageService.SaveFileAsync(signedContract.OpenReadStream(), fileName, "signed-contracts");

            // Update contract
            contract.StudentSignedContractPath = filePath;
            contract.IsStudentSigned = true;
            contract.StudentSignedAt = DateTime.UtcNow;
            
            // Update booking status based on landlord signature status
            if (contract.IsLandlordSigned)
            {
                booking.BookingStatus = BookingStatus.WaitingForAdminApproval;
                contract.ContractStatus = ContractStatus.WaitingForAdminApproval;
            }
            else
            {
                booking.BookingStatus = BookingStatus.WaitingForLandlordSignature;
                contract.ContractStatus = ContractStatus.WaitingForLandlordSignature;
            }
            
            booking.UpdatedAt = DateTime.UtcNow;
            contract.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Contracts.Update(contract);
            await _unitOfWork.Bookings.Update(booking);
            await _unitOfWork.SaveChangesAsync();

            // Notify landlord
            try
            {
                // Get landlord from booking
                Guid? landlordId = null;
                if (booking.BedId.HasValue)
                {
                    var bed = await _unitOfWork.Beds.GetAsync(booking.BedId.Value);
                    if (bed?.Room?.HousingUnit?.LandLordId != null)
                        landlordId = bed.Room.HousingUnit.LandLordId;
                }
                else if (booking.RoomId.HasValue)
                {
                    var room = await _unitOfWork.Rooms.GetAsync(booking.RoomId.Value);
                    if (room?.HousingUnit?.LandLordId != null)
                        landlordId = room.HousingUnit.LandLordId;
                }
                else if (booking.HousingUnitId.HasValue)
                {
                    var housingUnit = await _unitOfWork.HousingUnits.GetAsync(booking.HousingUnitId.Value);
                    if (housingUnit?.LandLordId != null)
                        landlordId = housingUnit.LandLordId;
                }

                var landlord = await _unitOfWork.LandLords.GetAsync(landlordId ?? Guid.Empty);
                if (contract.IsLandlordSigned)
                {
                    // Both signed - notify admin
                    await _notificationService.SendNotificationToRoleAsync(
                        "Admin",
                        $"Both parties have signed contract for booking {bookingId}. Please review and approve.",
                        NotificationTypes.ContractSigned);
                }
                else
                {
                    if (!string.IsNullOrEmpty(landlord?.UserId))
                        await _notificationService.SendRealTimeNotificationAsync(
                            landlord.UserId,
                            "Student has signed the contract. Please review and sign.",
                            NotificationTypes.StudentSigned);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification");
            }

            return Ok(new
            {
                Message = "Student signature uploaded successfully",
                ContractId = contract.ContractId,
                FilePath = filePath,
                BookingStatus = booking.BookingStatus.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading student signature");
            return StatusCode(500, new { Message = $"Error uploading signature: {ex.Message}" });
        }
    }

    /// <summary>
    /// Upload signed contract by landlord
    /// </summary>
    [HttpPost("{bookingId}/upload-landlord-signature")]
    [Authorize(Roles = "LandLord")]
    public async Task<IActionResult> UploadLandlordSignature(Guid bookingId, IFormFile signedContract)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not identified" });

            var booking = await _unitOfWork.Bookings.GetAsync(bookingId);
            if (booking == null)
                return NotFound(new { Message = "Booking not found" });

            // Get landlord from booking
            Guid? landlordId = null;
            if (booking.BedId.HasValue)
            {
                var bed = await _unitOfWork.Beds.GetAsync(booking.BedId.Value);
                if (bed?.Room?.HousingUnit?.LandLordId != null)
                    landlordId = bed.Room.HousingUnit.LandLordId;
            }
            else if (booking.RoomId.HasValue)
            {
                var room = await _unitOfWork.Rooms.GetAsync(booking.RoomId.Value);
                if (room?.HousingUnit?.LandLordId != null)
                    landlordId = room.HousingUnit.LandLordId;
            }
            else if (booking.HousingUnitId.HasValue)
            {
                var housingUnit = await _unitOfWork.HousingUnits.GetAsync(booking.HousingUnitId.Value);
                if (housingUnit?.LandLordId != null)
                    landlordId = housingUnit.LandLordId;
            }

            // Verify landlord owns this booking
            var landlord = await _unitOfWork.LandLords.GetAsync(landlordId ?? Guid.Empty);
            if (landlord?.UserId != userId)
                return Forbid("You can only sign your own bookings");

            if (booking.BookingStatus != BookingStatus.WaitingForSignatures && 
                booking.BookingStatus != BookingStatus.WaitingForStudentSignature)
                return BadRequest(new { Message = $"Booking is not ready for landlord signature. Current status: {booking.BookingStatus}" });

            if (signedContract == null || signedContract.Length == 0)
                return BadRequest(new { Message = "Signed contract file is required" });

            if (!signedContract.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { Message = "Only PDF files are allowed" });

            var contract = await _unitOfWork.Contracts.GetByBookingIdAsync(bookingId);
            if (contract == null)
                return NotFound(new { Message = "Contract not found" });

            // Upload signed contract
            var fileName = $"landlord_signed_{bookingId}_{Guid.NewGuid()}.pdf";
            var filePath = await _fileStorageService.SaveFileAsync(signedContract.OpenReadStream(), fileName, "signed-contracts");

            // Update contract
            contract.LandlordSignedContractPath = filePath;
            contract.IsLandlordSigned = true;
            contract.LandlordSignedAt = DateTime.UtcNow;
            
            // Update booking status based on student signature status
            if (contract.IsStudentSigned)
            {
                booking.BookingStatus = BookingStatus.WaitingForAdminApproval;
                contract.ContractStatus = ContractStatus.WaitingForAdminApproval;
            }
            else
            {
                booking.BookingStatus = BookingStatus.WaitingForStudentSignature;
                contract.ContractStatus = ContractStatus.WaitingForStudentSignature;
            }
            
            booking.UpdatedAt = DateTime.UtcNow;
            contract.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Contracts.Update(contract);
            await _unitOfWork.Bookings.Update(booking);
            await _unitOfWork.SaveChangesAsync();

            // Notify student
            try
            {
                var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
                if (contract.IsStudentSigned)
                {
                    // Both signed - notify admin
                    await _notificationService.SendNotificationToRoleAsync(
                        "Admin",
                        $"Both parties have signed contract for booking {bookingId}. Please review and approve.",
                        NotificationTypes.ContractSigned);
                }
                else
                {
                    if (!string.IsNullOrEmpty(student?.UserId))
                        await _notificationService.SendRealTimeNotificationAsync(
                            student.UserId,
                            "Landlord has signed the contract. Please review and sign.",
                            NotificationTypes.LandlordSigned);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification");
            }

            return Ok(new
            {
                Message = "Landlord signature uploaded successfully",
                ContractId = contract.ContractId,
                FilePath = filePath,
                BookingStatus = booking.BookingStatus.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading landlord signature");
            return StatusCode(500, new { Message = $"Error uploading signature: {ex.Message}" });
        }
    }

    // Helper methods

    private async Task<Guid?> GetStudentIdFromUserId(string userId)
    {
        var student = await _unitOfWork.Students.GetAll()
            .FirstOrDefaultAsync(s => s.UserId == userId);
        return student?.StudentId;
    }

    private async Task<Guid?> GetLandlordIdFromUserId(string userId)
    {
        var landlord = await _unitOfWork.LandLords.GetAll()
            .FirstOrDefaultAsync(l => l.UserId == userId);
        return landlord?.LandLordId;
    }
}
