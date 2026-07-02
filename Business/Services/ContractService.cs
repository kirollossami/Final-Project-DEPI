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
using System.Globalization;
using System.Text;

namespace Business.Services;

public class ContractService : IContractService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<ContractService> _logger;

    public ContractService(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        ILogger<ContractService> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<ContractResponse> GenerateContractAsync(ContractGenerationRequest request)
    {
        try
        {
            var contract = new Contract
            {
                ContractId = Guid.NewGuid(),
                BookingId = request.BookingId,
                ContractNumber = $"CTR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                ReceivingDate = request.ReceivingDate,
                HandoverDate = request.HandoverDate,
                FinalPrice = request.FinalPrice,
                DurationType = request.DurationType,
                DurationValue = request.DurationValue,
                OwnerFullName = request.OwnerFullName,
                OwnerNationalId = request.OwnerNationalId,
                StudentFullName = request.StudentFullName,
                StudentNationalId = request.StudentNationalId
            };

            // Generate PDF
            var pdfBytes = GenerateContractPdfContent(contract);
            var pdfFileName = $"contract_{contract.ContractNumber}.pdf";
            var pdfUrl = await _fileStorageService.SaveFileAsync(new MemoryStream(pdfBytes), pdfFileName, "contracts");

            contract.GeneratedPdfUrl = pdfUrl;

            await _unitOfWork.Contracts.Insert(contract);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Contract generated successfully: {contract.ContractNumber}");

            return new ContractResponse
            {
                ContractId = contract.ContractId,
                ContractNumber = contract.ContractNumber,
                GeneratedPdfUrl = contract.GeneratedPdfUrl,
                IsStudentSigned = contract.IsStudentSigned,
                IsOwnerSigned = contract.IsOwnerSigned,
                IsAdminApproved = contract.IsAdminApproved,
                CreatedAt = contract.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating contract");
            throw;
        }
    }

    public async Task<ContractResponse> SignContractAsync(ContractSignatureRequest request)
    {
        try
        {
            var contract = await _unitOfWork.Contracts.GetAsync(request.ContractId);
            if (contract == null)
            {
                throw new ArgumentException("Contract not found");
            }

            if (request.Role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                contract.StudentSignedPdfUrl = request.SignedPdfUrl;
                contract.IsStudentSigned = true;
                contract.StudentSignedAt = DateTime.UtcNow;
            }
            else if (request.Role.Equals("Owner", StringComparison.OrdinalIgnoreCase))
            {
                contract.OwnerSignedPdfUrl = request.SignedPdfUrl;
                contract.IsOwnerSigned = true;
                contract.OwnerSignedAt = DateTime.UtcNow;
            }
            else
            {
                throw new ArgumentException("Invalid role. Must be 'Student' or 'Owner'");
            }

            contract.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Contracts.Update(contract);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Contract signed by {request.Role}: {contract.ContractNumber}");

            return new ContractResponse
            {
                ContractId = contract.ContractId,
                ContractNumber = contract.ContractNumber,
                GeneratedPdfUrl = contract.GeneratedPdfUrl,
                IsStudentSigned = contract.IsStudentSigned,
                IsOwnerSigned = contract.IsOwnerSigned,
                IsAdminApproved = contract.IsAdminApproved,
                CreatedAt = contract.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing contract");
            throw;
        }
    }

    public async Task<ContractResponse?> GetContractByIdAsync(Guid contractId)
    {
        var contract = await _unitOfWork.Contracts.GetAsync(contractId);
        if (contract == null) return null;

        return new ContractResponse
        {
            ContractId = contract.ContractId,
            ContractNumber = contract.ContractNumber,
            GeneratedPdfUrl = contract.GeneratedPdfUrl,
            IsStudentSigned = contract.IsStudentSigned,
            IsOwnerSigned = contract.IsOwnerSigned,
            IsAdminApproved = contract.IsAdminApproved,
            CreatedAt = contract.CreatedAt
        };
    }

    public async Task<ContractResponse?> GetContractByBookingIdAsync(Guid bookingId)
    {
        var contract = await _unitOfWork.Contracts.GetByBookingIdAsync(bookingId);
        if (contract == null) return null;

        return new ContractResponse
        {
            ContractId = contract.ContractId,
            ContractNumber = contract.ContractNumber,
            GeneratedPdfUrl = contract.GeneratedPdfUrl,
            IsStudentSigned = contract.IsStudentSigned,
            IsOwnerSigned = contract.IsOwnerSigned,
            IsAdminApproved = contract.IsAdminApproved,
            CreatedAt = contract.CreatedAt
        };
    }

    public async Task<byte[]> GenerateContractPdfAsync(Guid contractId)
    {
        var contract = await _unitOfWork.Contracts.GetAsync(contractId);
        if (contract == null)
        {
            throw new ArgumentException("Contract not found");
        }

        return GenerateContractPdfContent(contract);
    }

    private byte[] GenerateContractPdfContent(Contract contract)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));

                page.Header().Element(header =>
                {
                    header.Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("عقد إيجار").Bold().FontSize(20).FontColor(Colors.Blue.Darken2);
                            column.Item().Text($"رقم العقد: {contract.ContractNumber}").FontSize(10);
                            column.Item().Text($"تاريخ العقد: {contract.CreatedAt:dd/MM/yyyy}").FontSize(10);
                        });
                    });
                });

                page.Content().Element(content =>
                {
                    content.Column(column =>
                    {
                        column.Spacing(10);

                        // Title
                        column.Item().AlignCenter().Text("عقد إيجار سكن طلابي").Bold().FontSize(16);
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Preamble
                        column.Item().Text(GetArabicPreamble()).FontSize(11);
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // First Party (Owner)
                        column.Item().Text("الطرف الأول (المؤجر):").Bold().FontSize(13);
                        column.Item().Text($"الاسم الكامل: {contract.OwnerFullName}").FontSize(11);
                        column.Item().Text($"الرقم القومي: {contract.OwnerNationalId}").FontSize(11);
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Second Party (Student)
                        column.Item().Text("الطرف الثاني (المستأجر):").Bold().FontSize(13);
                        column.Item().Text($"الاسم الكامل: {contract.StudentFullName}").FontSize(11);
                        column.Item().Text($"الرقم القومي: {contract.StudentNationalId}").FontSize(11);
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Contract Terms
                        column.Item().Text("بنود العقد:").Bold().FontSize(13);
                        column.Item().Text(GetArabicContractTerms(contract)).FontSize(11);
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Financial Details
                        column.Item().Text("التفاصيل المالية:").Bold().FontSize(13);
                        column.Item().Text($"الإجمالي النهائي: {contract.FinalPrice:N2} جنيه مصري").FontSize(11);
                        column.Item().Text($"تاريخ الاستلام: {contract.ReceivingDate:dd/MM/yyyy}").FontSize(11);
                        column.Item().Text($"تاريخ التسليم: {contract.HandoverDate:dd/MM/yyyy}").FontSize(11);
                        
                        var durationText = contract.DurationType == ContractDurationType.Monthly
                            ? $"{contract.DurationValue} شهر"
                            : $"{contract.DurationValue} سنة";
                        column.Item().Text($"مدة العقد: {durationText}").FontSize(11);
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Signatures Section
                        column.Item().Text("التوقيعات:").Bold().FontSize(13);
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("توقيع الطرف الأول (المؤجر):").FontSize(11);
                                col.Item().Height(3, Unit.Centimetre).Border(1).BorderColor(Colors.Grey.Lighten1);
                            });
                            row.ConstantItem(50).Column(col => { });
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("توقيع الطرف الثاني (المستأجر):").FontSize(11);
                                col.Item().Height(3, Unit.Centimetre).Border(1).BorderColor(Colors.Grey.Lighten1);
                            });
                        });

                        // Footer
                        column.Item().PaddingTop(20).AlignCenter()
                            .Text("هذا العقد ملزم لكلا الطرفين وفقاً للقانون المصري").FontSize(9).Italic();
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

    private string GetArabicPreamble()
    {
        return "تم هذا العقد في اليوم .../.../.... بموجب القانون المدني المصري، بين الطرفين التاليين:";
    }

    private string GetArabicContractTerms(Contract contract)
    {
        var terms = new StringBuilder();
        terms.AppendLine("1. يتعهد الطرف الأول (المؤجر) بتسليم الوحدة السكنية للطرف الثاني في حالة جيدة وصالحة للسكن.");
        terms.AppendLine("2. يتعهد الطرف الثاني (المستأجر) بدفع المبلغ المتفق عليه في المواعيد المحددة.");
        terms.AppendLine("3. يلتزم الطرف الثاني بالمحافظة على الوحدة السكنية ومحتوياتها وعدم إحداث أي تلفيات.");
        terms.AppendLine("4. لا يحق للطرف الثاني تأجير الوحدة السكنية للغير دون موافقة كتابية من الطرف الأول.");
        terms.AppendLine("5. يلتزم الطرف الثاني بدفع فواتير الخدمات (كهرباء، مياه، غاز) المستحقة عليه.");
        terms.AppendLine("6. يحق للطرف الأول فسخ العقد في حالة مخالفة الطرف الثاني لأي من بنود هذا العقد.");
        terms.AppendLine("7. يلتزم الطرف الثاني بإعادة الوحدة السكنية في نفس الحالة التي استلمها عليها عند انتهاء العقد.");
        terms.AppendLine("8. يعتبر هذا العقد ساري المفعول من تاريخ التوقيع عليه من كلا الطرفين.");
        terms.AppendLine("9. أي نزاع ينشأ عن تنفيذ هذا العقد يرجع للقضاء المختص وفقاً للقانون المصري.");
        terms.AppendLine("10. حرر هذا العقد من نسختين، لكل طرف نسخة، ولها نفس الحجية القانونية.");
        
        return terms.ToString();
    }
}
