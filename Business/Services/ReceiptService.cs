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

    public async Task<ReceiptResponse> GenerateReceiptAsync(ReceiptGenerationRequest request)
    {
        try
        {
            var payment = await _unitOfWork.Payments.GetAsync(request.PaymentId);
            if (payment == null)
            {
                throw new ArgumentException("Payment not found");
            }

            var escrow = await _unitOfWork.EscrowTransactions.GetAsync(request.EscrowId);
            if (escrow == null)
            {
                throw new ArgumentException("Escrow transaction not found");
            }

            var receipt = new PaymentReceipt
            {
                ReceiptId = Guid.NewGuid(),
                PaymentId = request.PaymentId,
                EscrowId = request.EscrowId,
                ReceiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
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
                    EscrowId = escrow.EscrowId,
                    Type = request.Type.ToString(),
                    Amount = payment.Amount,
                    Currency = "EGP",
                    PaymentDate = payment.PaymentDate,
                    PaymentStatus = payment.PaymentStatus.ToString(),
                    AdditionalData = request.AdditionalData
                }),
                CreatedAt = DateTime.UtcNow
            };

            // Generate PDF
            var pdfBytes = GenerateReceiptPdfContent(receipt, payment, escrow);
            var pdfFileName = $"receipt_{receipt.ReceiptNumber}.pdf";
            var pdfUrl = await _fileStorageService.SaveFileAsync(new MemoryStream(pdfBytes), pdfFileName, "receipts");

            receipt.ReceiptPdfUrl = pdfUrl;

            await _unitOfWork.PaymentReceipts.Insert(receipt);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Receipt generated: {receipt.ReceiptNumber}");

            return MapToResponse(receipt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating receipt");
            throw;
        }
    }

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

    public async Task<byte[]> GenerateReceiptPdfAsync(Guid receiptId)
    {
        var receipt = await _unitOfWork.PaymentReceipts.GetAsync(receiptId);
        if (receipt == null)
        {
            throw new ArgumentException("Receipt not found");
        }

        var payment = await _unitOfWork.Payments.GetAsync(receipt.PaymentId);
        var escrow = await _unitOfWork.EscrowTransactions.GetAsync(receipt.EscrowId);

        return GenerateReceiptPdfContent(receipt, payment, escrow);
    }

    private byte[] GenerateReceiptPdfContent(PaymentReceipt receipt, Payment? payment, EscrowTransaction? escrow)
    {
        var document = Document.Create(container =>
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
                            column.Item().Text("إيصال دفع / Payment Receipt").Bold().FontSize(18).FontColor(Colors.Blue.Darken2);
                            column.Item().Text($"رقم الإيصال: {receipt.ReceiptNumber}").FontSize(10);
                            column.Item().Text($"تاريخ الإصدار: {receipt.CreatedAt:dd/MM/yyyy HH:mm}").FontSize(10);
                        });
                    });
                });

                page.Content().Element(content =>
                {
                    content.Column(column =>
                    {
                        column.Spacing(15);

                        // Receipt Type
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("نوع الإيصال:").Bold();
                            row.RelativeItem().Text(GetReceiptTypeArabic(receipt.Type));
                        });

                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Issued To
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("صادر إلى:").Bold();
                            row.RelativeItem().Text(receipt.IssuedToName);
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("الصفة:").Bold();
                            row.RelativeItem().Text(GetRoleArabic(receipt.IssuedToRole));
                        });

                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Amount
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("المبلغ:").Bold();
                            row.RelativeItem().Text($"{receipt.Amount:N2} {receipt.Currency}").Bold().FontSize(14).FontColor(Colors.Green.Darken2);
                        });

                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Transaction Details
                        column.Item().Text("تفاصيل المعاملة:").Bold().FontSize(12);
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("رقم المعاملة:");
                            row.RelativeItem().Text(receipt.TransactionReference);
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("طريقة الدفع:");
                            row.RelativeItem().Text(receipt.PaymentMethod);
                        });

                        if (payment != null)
                        {
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("تاريخ الدفع:");
                                row.RelativeItem().Text(payment.PaymentDate.ToString("dd/MM/yyyy HH:mm"));
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("حالة الدفع:");
                                row.RelativeItem().Text(GetPaymentStatusArabic(payment.PaymentStatus));
                            });
                        }

                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Escrow Details
                        if (escrow != null)
                        {
                            column.Item().Text("تفاصيل الحساب الأماني (Escrow):").Bold().FontSize(12);
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("المبلغ المحتجز:");
                                row.RelativeItem().Text($"{escrow.HeldAmount:N2} {escrow.Currency}");
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("الحالة:");
                                row.RelativeItem().Text(GetEscrowStatusArabic(escrow.Status));
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("رسوم المنصة:");
                                row.RelativeItem().Text($"{escrow.PlatformFee:N2} {escrow.Currency}");
                            });

                            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        }

                        // Footer
                        column.Item().PaddingTop(30).AlignCenter()
                            .Text("هذا الإيصال صادر عن منصة حجز السكن الطلابي").FontSize(9).Italic();
                        column.Item().AlignCenter()
                            .Text("This receipt is issued by Student Housing Booking Platform").FontSize(9).Italic();
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("صفحة ");
                    x.CurrentPageNumber();
                });
            });
        });

        return document.GeneratePdf();
    }

    private ReceiptResponse MapToResponse(PaymentReceipt receipt)
    {
        return new ReceiptResponse
        {
            ReceiptId = receipt.ReceiptId,
            ReceiptNumber = receipt.ReceiptNumber,
            Amount = receipt.Amount,
            Currency = receipt.Currency,
            Type = receipt.Type,
            IssuedToName = receipt.IssuedToName,
            IssuedToRole = receipt.IssuedToRole,
            ReceiptPdfUrl = receipt.ReceiptPdfUrl,
            CreatedAt = receipt.CreatedAt
        };
    }

    private string GetReceiptTypeArabic(ReceiptType type)
    {
        return type switch
        {
            ReceiptType.PaymentReceived => "تم استلام الدفع",
            ReceiptType.EscrowHeld => "حجز في الحساب الأماني",
            ReceiptType.EscrowReleased => "إطلاق من الحساب الأماني",
            ReceiptType.RefundIssued => "إصدار استرداد",
            ReceiptType.OwnerPayout => "دفع للمالك",
            _ => type.ToString()
        };
    }

    private string GetRoleArabic(string role)
    {
        return role switch
        {
            "Student" => "طالب",
            "Owner" => "مالك",
            "Admin" => "مدير النظام",
            _ => role
        };
    }

    private string GetPaymentStatusArabic(PaymentStatus status)
    {
        return status switch
        {
            PaymentStatus.Pending => "قيد الانتظار",
            PaymentStatus.Completed => "مكتمل",
            PaymentStatus.Failed => "فشل",
            _ => status.ToString()
        };
    }

    private string GetEscrowStatusArabic(EscrowStatus status)
    {
        return status switch
        {
            EscrowStatus.Holding => "محتجز",
            EscrowStatus.Released => "مطلق",
            EscrowStatus.Refunded => "مسترد",
            EscrowStatus.PartiallyReleased => "مطلق جزئياً",
            _ => status.ToString()
        };
    }
}
