using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;

namespace Business.Services;

/// <summary>
/// Manages contracts that are manually uploaded as PDF files by the admin.
/// No PDF is auto-generated — the admin prepares and uploads the contract file.
/// </summary>
public class ContractService : IContractService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IEscrowService _escrowService;
    private readonly ILogger<ContractService> _logger;
    private const decimal PlatformFeePercentage = 5.0m; // 5% platform fee

    public ContractService(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        IEscrowService escrowService,
        ILogger<ContractService> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _escrowService = escrowService;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Admin uploads the contract PDF manually
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<ContractResponse> UploadContractAsync(Guid bookingId, Stream pdfStream, string fileName)
    {
        try
        {
            var booking = await _unitOfWork.Bookings.GetAsync(bookingId);
            if (booking == null)
                throw new ArgumentException($"Booking {bookingId} not found");

            var payment = await _unitOfWork.Payments.GetByBookingIdAsync(bookingId);
            bool isPaymentCompleted = payment != null && payment.PaymentStatus == PaymentStatus.Completed;

            if (booking.BookingStatus != BookingStatus.WaitingForContract && !(booking.BookingStatus == BookingStatus.PendingPayment && isPaymentCompleted))
                throw new InvalidOperationException($"Booking must be in WaitingForContract status. Current: {booking.BookingStatus}");

            // Store the uploaded PDF
            var storedFileName = $"contract_{bookingId}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            var pdfUrl = await _fileStorageService.SaveFileAsync(pdfStream, storedFileName, "contracts");

            var contract = new Contract
            {
                ContractId = Guid.NewGuid(),
                BookingId = bookingId,
                ContractNumber = $"CTR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                OriginalContractPdfPath = pdfUrl,
                ContractStatus = ContractStatus.WaitingForSignatures,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Contracts.Insert(contract);
            await _unitOfWork.SaveChangesAsync();

            // Create escrow transaction when contract is uploaded
            if (payment != null)
            {
                // Check if escrow already exists for this payment
                var existingEscrow = await _unitOfWork.EscrowTransactions.GetByPaymentIdAsync(payment.PaymentId);
                if (existingEscrow == null)
                {
                    // Create new escrow linked to this contract
                    await _escrowService.CreateEscrowAsync(payment.PaymentId, contract.ContractId, PlatformFeePercentage);
                    _logger.LogInformation("Escrow created for payment {PaymentId} and contract {ContractId}",
                        payment.PaymentId, contract.ContractId);
                }
                else
                {
                    // Link existing escrow to this contract
                    existingEscrow.ContractId = contract.ContractId;
                    await _unitOfWork.EscrowTransactions.Update(existingEscrow);
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("Existing escrow {EscrowId} linked to contract {ContractId}",
                        existingEscrow.EscrowId, contract.ContractId);
                }
            }

            // Update booking status
            booking.BookingStatus = BookingStatus.WaitingForSignatures;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.Update(booking);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Contract {Number} uploaded for booking {BookingId}, status updated to WaitingForSignatures",
                contract.ContractNumber, bookingId);

            return MapToResponse(contract);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading contract for booking {BookingId}", bookingId);
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Sign contract (student or landlord uploads signed PDF)
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<ContractResponse> SignContractAsync(ContractSignatureRequest request)
    {
        try
        {
            var contract = await _unitOfWork.Contracts.GetAsync(request.ContractId);
            if (contract == null)
                throw new ArgumentException("Contract not found");

            var booking = await _unitOfWork.Bookings.GetAsync(contract.BookingId);

            var signingAllowedStatuses = new[]
            {
                BookingStatus.WaitingForSignatures,
                BookingStatus.WaitingForStudentSignature,
                BookingStatus.WaitingForLandlordSignature
            };

            if (booking != null && !signingAllowedStatuses.Contains(booking.BookingStatus))
                throw new InvalidOperationException(
                    $"Booking is not in a signable state. Current status: {booking.BookingStatus}");

            if (request.Role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                if (contract.IsStudentSigned)
                    throw new InvalidOperationException("Student has already signed this contract.");

                contract.StudentSignedContractPath = request.SignedPdfUrl;
                contract.IsStudentSigned = true;
                contract.StudentSignedAt = DateTime.UtcNow;

                if (booking != null)
                {
                    booking.BookingStatus = contract.IsLandlordSigned
                        ? BookingStatus.WaitingForAdminApproval
                        : BookingStatus.WaitingForLandlordSignature;
                    booking.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Bookings.Update(booking);
                    _logger.LogInformation("Booking {Id} → {Status} after student signed",
                        booking.BookingId, booking.BookingStatus);
                }
            }
            else if (request.Role.Equals("Owner", StringComparison.OrdinalIgnoreCase))
            {
                if (contract.IsLandlordSigned)
                    throw new InvalidOperationException("Owner has already signed this contract.");

                contract.LandlordSignedContractPath = request.SignedPdfUrl;
                contract.IsLandlordSigned = true;
                contract.LandlordSignedAt = DateTime.UtcNow;

                if (booking != null)
                {
                    booking.BookingStatus = contract.IsStudentSigned
                        ? BookingStatus.WaitingForAdminApproval
                        : BookingStatus.WaitingForStudentSignature;
                    booking.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Bookings.Update(booking);
                    _logger.LogInformation("Booking {Id} → {Status} after owner signed",
                        booking.BookingId, booking.BookingStatus);
                }
            }
            else
            {
                throw new ArgumentException("Invalid role. Must be 'Student' or 'Owner'");
            }

            contract.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Contracts.Update(contract);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Contract {Number} signed by {Role}", contract.ContractNumber, request.Role);
            return MapToResponse(contract);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing contract");
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Queries
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<ContractResponse?> GetContractByIdAsync(Guid contractId)
    {
        var contract = await _unitOfWork.Contracts.GetAsync(contractId);
        return contract == null ? null : MapToResponse(contract);
    }

    public async Task<ContractResponse?> GetContractByBookingIdAsync(Guid bookingId)
    {
        var contract = await _unitOfWork.Contracts.GetByBookingIdAsync(bookingId);
        return contract == null ? null : MapToResponse(contract);
    }

    public async Task<byte[]> GetContractPdfAsync(Guid contractId)
    {
        var contract = await _unitOfWork.Contracts.GetAsync(contractId);
        if (contract == null)
            throw new ArgumentException("Contract not found");

        // Return the most recent signed version, or the original uploaded file
        var path = contract.LandlordSignedContractPath
                ?? contract.StudentSignedContractPath
                ?? contract.OriginalContractPdfPath;

        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException("No contract PDF is available yet");

        var file = await _fileStorageService.GetFileAsync(path);
        if (file == null)
            throw new InvalidOperationException("Contract PDF file not found in storage");

        return file.Value.Content;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Mapping
    // ─────────────────────────────────────────────────────────────────────────
    private static ContractResponse MapToResponse(Contract contract) => new ContractResponse
    {
        ContractId = contract.ContractId,
        BookingId = contract.BookingId,
        ContractNumber = contract.ContractNumber,
        OriginalContractPdfPath = contract.OriginalContractPdfPath,
        StudentSignedContractPath = contract.StudentSignedContractPath,
        LandlordSignedContractPath = contract.LandlordSignedContractPath,
        IsStudentSigned = contract.IsStudentSigned,
        IsLandlordSigned = contract.IsLandlordSigned,
        IsAdminApproved = contract.IsAdminApproved,
        StudentSignedAt = contract.StudentSignedAt,
        LandlordSignedAt = contract.LandlordSignedAt,
        AdminApprovedAt = contract.AdminApprovedAt,
        AdminNotes = contract.AdminNotes,
        ContractStatus = contract.ContractStatus,
        CreatedAt = contract.CreatedAt
    };
}
