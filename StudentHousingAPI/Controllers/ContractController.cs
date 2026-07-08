using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Business.Interfaces;
using Business.DTOs.Requests;

namespace StudentHousingAPI.Controllers
{
    [ApiController]
    [Route("api/contracts")]
    public class ContractController : ControllerBase
    {
        private readonly IContractService _contractService;
        private readonly IAdminApprovalService _adminApprovalService;
        private readonly INotificationService _notificationService;

        public ContractController(
            IContractService contractService,
            IAdminApprovalService adminApprovalService,
            INotificationService notificationService)
        {
            _contractService = contractService;
            _adminApprovalService = adminApprovalService;
            _notificationService = notificationService;
        }

        // GET api/contracts/{id} – download contract PDF
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Download(Guid id)
        {
            var pdfBytes = await _contractService.GenerateContractPdfAsync(id);
            if (pdfBytes == null || pdfBytes.Length == 0) return NotFound();
            return File(pdfBytes, "application/pdf", $"contract-{id}.pdf");
        }

        // POST api/contracts/{id}/signatures/student – upload student signature URL
        [HttpPost("{id:guid}/signatures/student")]
        public async Task<IActionResult> UploadStudentSignature(Guid id, [FromBody] SignatureDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.SignedPdfUrl))
                return BadRequest("Signed PDF URL is required.");

            var request = new ContractSignatureRequest
            {
                ContractId = id,
                SignedPdfUrl = dto.SignedPdfUrl,
                Role = "Student"
            };

            await _contractService.SignContractAsync(request);
            return NoContent();
        }

        // POST api/contracts/{id}/signatures/owner – upload owner signature URL
        [HttpPost("{id:guid}/signatures/owner")]
        public async Task<IActionResult> UploadOwnerSignature(Guid id, [FromBody] SignatureDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.SignedPdfUrl))
                return BadRequest("Signed PDF URL is required.");

            var request = new ContractSignatureRequest
            {
                ContractId = id,
                SignedPdfUrl = dto.SignedPdfUrl,
                Role = "Owner"
            };

            await _contractService.SignContractAsync(request);
            return NoContent();
        }

        // POST api/contracts/{id}/admin/approve – admin approves contract
        [HttpPost("{id:guid}/admin/approve")]
        public async Task<IActionResult> Approve(Guid id, [FromBody] AdminActionDto dto)
        {
            var request = new AdminContractApprovalRequest
            {
                ContractId = id,
                AdminUserId = dto?.AdminUserId ?? string.Empty,
                AdminNotes = dto?.Notes,
                IsApproved = true
            };

            var result = await _adminApprovalService.ApproveContractAsync(request);
            if (!result.Success) return BadRequest(result.Message);
            return NoContent();
        }

        // POST api/contracts/{id}/admin/reject – admin rejects contract
        [HttpPost("{id:guid}/admin/reject")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] AdminActionDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Notes))
                return BadRequest("Rejection reason is required.");

            var request = new AdminContractApprovalRequest
            {
                ContractId = id,
                AdminUserId = dto.AdminUserId ?? string.Empty,
                AdminNotes = dto.Notes,
                IsApproved = false
            };

            var result = await _adminApprovalService.RejectContractAsync(request);
            if (!result.Success) return BadRequest(result.Message);
            return NoContent();
        }

        // DTOs scoped to this controller
        public class SignatureDto { public string SignedPdfUrl { get; set; } = string.Empty; }
        public class AdminActionDto { public string? AdminUserId { get; set; } public string? Notes { get; set; } }
    }
}
