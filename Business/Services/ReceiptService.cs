using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace Business.Services;

public class ReceiptService : IReceiptService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<ReceiptService> _logger;

    public ReceiptService(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        ILogger<ReceiptService> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Payment receipt — does NOT require an escrow record
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<ReceiptResponse> GeneratePaymentReceiptAsync(ReceiptGenerationRequest request)
    {
        try
        {
            var payment = await _unitOfWork.Payments.GetAsync(request.PaymentId);
            if (payment == null)
                throw new ArgumentException($"Payment {request.PaymentId} not found");

            var receipt = BuildReceiptEntity(request, payment, escrow: null);

            var pdfBytes = GenerateReceiptPdf(receipt, payment, escrow: null);
            var pdfUrl = await _fileStorageService.SaveFileAsync(
                new MemoryStream(pdfBytes), $"receipt_{receipt.ReceiptNumber}.pdf", "receipts");
            receipt.ReceiptPdfUrl = pdfUrl;

            await _unitOfWork.PaymentReceipts.Insert(receipt);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("PaymentReceipt {ReceiptNumber} generated for user {UserId}",
                receipt.ReceiptNumber, receipt.IssuedToUserId);

            // Attach payment nav property so MapToResponse can populate BookingId / PaymentStatus
            receipt.Payment = payment;
            return MapToResponse(receipt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payment receipt");
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Escrow receipt — requires an existing escrow record
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<ReceiptResponse> GenerateEscrowReceiptAsync(ReceiptGenerationRequest request)
    {
        try
        {
            var payment = await _unitOfWork.Payments.GetAsync(request.PaymentId);
            if (payment == null)
                throw new ArgumentException($"Payment {request.PaymentId} not found");

            EscrowTransaction? escrow = null;
            if (request.EscrowId.HasValue && request.EscrowId.Value != Guid.Empty)
            {
                escrow = await _unitOfWork.EscrowTransactions.GetAsync(request.EscrowId.Value);
                if (escrow == null)
                    throw new ArgumentException($"Escrow {request.EscrowId} not found");
            }

            var receipt = BuildReceiptEntity(request, payment, escrow);

            var pdfBytes = GenerateReceiptPdf(receipt, payment, escrow);
            var pdfUrl = await _fileStorageService.SaveFileAsync(
                new MemoryStream(pdfBytes), $"receipt_{receipt.ReceiptNumber}.pdf", "receipts");
            receipt.ReceiptPdfUrl = pdfUrl;

            await _unitOfWork.PaymentReceipts.Insert(receipt);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("EscrowReceipt {ReceiptNumber} generated for user {UserId}",
                receipt.ReceiptNumber, receipt.IssuedToUserId);

            receipt.Payment = payment;
            receipt.EscrowTransaction = escrow;
            return MapToResponse(receipt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating escrow receipt");
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Queries
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<ReceiptResponse?> GetReceiptByIdAsync(Guid receiptId)
    {
        var receipt = await _unitOfWork.PaymentReceipts.GetAsync(receiptId);
        return receipt == null ? null : MapToResponse(receipt);
    }

    public async Task<IEnumerable<ReceiptResponse>> GetReceiptsByUserIdAsync(string userId)
    {
        var receipts = await _unitOfWork.PaymentReceipts.GetByUserIdAsync(userId);
        return receipts.Select(MapToResponse);
    }

    public async Task<IEnumerable<ReceiptResponse>> GetReceiptsByPaymentIdAsync(Guid paymentId)
    {
        // Get all receipts for a payment (student + landlord receipts share the same PaymentId)
        var receipts = await _unitOfWork.PaymentReceipts.GetAllByPaymentIdAsync(paymentId);
        return receipts.Select(MapToResponse);
    }

    public async Task<byte[]> GenerateReceiptPdfAsync(Guid receiptId)
    {
        var receipt = await _unitOfWork.PaymentReceipts.GetAsync(receiptId);
        if (receipt == null) throw new ArgumentException("Receipt not found");

        // Payment nav property is loaded by the overridden GetAsync in PaymentReceiptRepository.
        // Fall back to an explicit lookup if it's null (e.g. called from a different context).
        var payment = receipt.Payment
            ?? await _unitOfWork.Payments.GetAsync(receipt.PaymentId);

        EscrowTransaction? escrow = receipt.EscrowId.HasValue && receipt.EscrowId != Guid.Empty
            ? await _unitOfWork.EscrowTransactions.GetAsync(receipt.EscrowId.Value)
            : null;

        return GenerateReceiptPdf(receipt, payment, escrow);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Internal helpers
    // ─────────────────────────────────────────────────────────────────────────
    private static PaymentReceipt BuildReceiptEntity(
        ReceiptGenerationRequest request, Payment payment, EscrowTransaction? escrow)
    {
        return new PaymentReceipt
        {
            ReceiptId = Guid.NewGuid(),
            PaymentId = request.PaymentId,
            EscrowId = escrow?.EscrowId,   // null when no escrow (PaymentReceived receipts)
            ReceiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            Amount = payment.Amount,
            Currency = "EGP",
            Type = request.Type,
            IssuedToUserId = request.IssuedToUserId,
            IssuedToRole = request.IssuedToRole,
            IssuedToName = request.IssuedToName,
            TransactionReference = payment.TransactionId ?? "N/A",
            PaymentMethod = payment.PaymentMethod.ToString(),
            ReceiptData = JsonSerializer.Serialize(new
            {
                PaymentId = payment.PaymentId,
                EscrowId = escrow?.EscrowId,
                Type = request.Type.ToString(),
                Amount = payment.Amount,
                Currency = "EGP",
                PaymentDate = payment.PaymentDate,
                PaymentStatus = payment.PaymentStatus.ToString(),
                AdditionalData = request.AdditionalData
            }),
            CreatedAt = DateTime.UtcNow
        };
    }

    private byte[] GenerateReceiptPdf(
        PaymentReceipt receipt, Payment? payment, EscrowTransaction? escrow)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Element(header =>
                {
                    header.Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("Payment Receipt")
                                .Bold().FontSize(18).FontColor(Colors.Blue.Darken2);
                            column.Item().Text($"Receipt No: {receipt.ReceiptNumber}").FontSize(10);
                            column.Item().Text($"Issue Date: {receipt.CreatedAt:dd/MM/yyyy HH:mm}").FontSize(10);
                        });
                    });
                });

                page.Content().Element(content =>
                {
                    content.Column(column =>
                    {
                        column.Spacing(15);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Receipt Type:").Bold();
                            row.RelativeItem().Text(GetReceiptTypeLabel(receipt.Type));
                        });
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Issued To:").Bold();
                            row.RelativeItem().Text(receipt.IssuedToName);
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Role:").Bold();
                            row.RelativeItem().Text(receipt.IssuedToRole);
                        });
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Amount:").Bold();
                            row.RelativeItem()
                                .Text($"{receipt.Amount:N2} {receipt.Currency}")
                                .Bold().FontSize(14).FontColor(Colors.Green.Darken2);
                        });
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        column.Item().Text("Transaction Details:").Bold().FontSize(12);
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Transaction Reference:");
                            row.RelativeItem().Text(receipt.TransactionReference);
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Payment Method:");
                            row.RelativeItem().Text(receipt.PaymentMethod);
                        });

                        if (payment != null)
                        {
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Payment Date:");
                                row.RelativeItem().Text(payment.PaymentDate.ToString("dd/MM/yyyy HH:mm"));
                            });
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Payment Status:");
                                row.RelativeItem().Text(payment.PaymentStatus.ToString());
                            });
                        }

                        // Escrow section — only shown when escrow data is available
                        if (escrow != null)
                        {
                            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            column.Item().Text("Escrow Details:").Bold().FontSize(12);
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Held Amount:");
                                row.RelativeItem().Text($"{escrow.Amount:N2} {escrow.Currency}");
                            });
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Escrow Status:");
                                row.RelativeItem().Text(escrow.Status.ToString());
                            });
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Platform Fee:");
                                row.RelativeItem().Text($"{escrow.PlatformFee:N2} {escrow.Currency}");
                            });
                        }

                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        column.Item().PaddingTop(30).AlignCenter()
                            .Text("This receipt is issued by Student Housing Booking Platform")
                            .FontSize(9).Italic();
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("صفحة ");
                    x.CurrentPageNumber();
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static ReceiptResponse MapToResponse(PaymentReceipt receipt) => new ReceiptResponse
    {
        ReceiptId = receipt.ReceiptId,
        PaymentId = receipt.PaymentId,
        BookingId = receipt.Payment?.BookingId ?? Guid.Empty,
        ReceiptNumber = receipt.ReceiptNumber,
        Amount = receipt.Amount,
        Currency = receipt.Currency,
        Type = receipt.Type,
        TypeLabel = GetReceiptTypeLabel(receipt.Type),
        IssuedToUserId = receipt.IssuedToUserId,
        IssuedToName = receipt.IssuedToName,
        IssuedToRole = receipt.IssuedToRole,
        TransactionReference = receipt.TransactionReference,
        PaymentMethod = receipt.PaymentMethod,
        PaymentStatus = receipt.Payment?.PaymentStatus.ToString() ?? "Unknown",
        PaymentDate = receipt.Payment?.PaymentDate,
        ReceiptPdfUrl = $"/api/Receipt/{receipt.ReceiptId}/download",
        CreatedAt = receipt.CreatedAt
    };

    private static string GetReceiptTypeArabic(ReceiptType type) => type switch
    {
        ReceiptType.PaymentReceived => "تم استلام الدفع",
        ReceiptType.EscrowHeld => "حجز في الحساب الأماني",
        ReceiptType.EscrowReleased => "إطلاق من الحساب الأماني",
        ReceiptType.RefundIssued => "إصدار استرداد",
        ReceiptType.OwnerPayout => "دفع للمالك",
        _ => type.ToString()
    };

    private static string GetRoleArabic(string role) => role switch
    {
        "Student" => "طالب",
        "LandLord" or "Owner" => "مالك",
        "Admin" => "مدير النظام",
        _ => role
    };

    private static string GetPaymentStatusArabic(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "قيد الانتظار",
        PaymentStatus.Completed => "مكتمل",
        PaymentStatus.Failed => "فشل",
        _ => status.ToString()
    };

    private static string GetEscrowStatusArabic(EscrowStatus status) => status switch
    {
        EscrowStatus.Holding => "محتجز",
        EscrowStatus.Released => "مطلق",
        EscrowStatus.Refunded => "مسترد",
        EscrowStatus.PartiallyReleased => "مطلق جزئياً",
        _ => status.ToString()
    };

    private static string GetReceiptTypeLabel(ReceiptType type) => type switch
    {
        ReceiptType.PaymentReceived => "Payment Received",
        ReceiptType.EscrowHeld => "Escrow Held",
        ReceiptType.EscrowReleased => "Escrow Released",
        ReceiptType.RefundIssued => "Refund Issued",
        ReceiptType.OwnerPayout => "Owner Payout",
        _ => type.ToString()
    };
}
