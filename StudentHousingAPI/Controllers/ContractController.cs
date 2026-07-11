using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Business.Interfaces;
using Business.DTOs.Requests;

namespace StudentHousingAPI.Controllers
{
    [ApiController]
    [Route("api/contracts")]
    [Authorize]
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

        // GET api/contracts/{id} – get contract details (JSON)
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var contract = await _contractService.GetContractByIdAsync(id);
            if (contract == null) return NotFound(new { Message = "Contract not found" });
            return Ok(contract);
        }

        // GET api/contracts/by-booking/{bookingId} – get contract for a booking
        [HttpGet("by-booking/{bookingId:guid}")]
        public async Task<IActionResult> GetByBookingId(Guid bookingId)
        {
            var contract = await _contractService.GetContractByBookingIdAsync(bookingId);
            if (contract == null) return NotFound(new { Message = "Contract not found for this booking" });
            return Ok(contract);
        }

        // GET api/contracts/{id}/pdf – download contract PDF
        [HttpGet("{id:guid}/pdf")]
        public async Task<IActionResult> DownloadPdf(Guid id)
        {
            var pdfBytes = await _contractService.GetContractPdfAsync(id);
            if (pdfBytes == null || pdfBytes.Length == 0) return NotFound();
            return File(pdfBytes, "application/pdf", $"contract-{id}.pdf");
        }

        // POST api/contracts/{id}/signatures/student – student signs contract
        [HttpPost("{id:guid}/signatures/student")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> UploadStudentSignature(Guid id, [FromBody] SignatureDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.SignedPdfUrl))
                return BadRequest(new { Message = "Signed PDF URL is required." });

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not identified." });

            var request = new ContractSignatureRequest
            {
                ContractId = id,
                SignedPdfUrl = dto.SignedPdfUrl,
                Role = "Student",
                UserId = userId
            };

            var result = await _contractService.SignContractAsync(request);
            return Ok(result);
        }

        // POST api/contracts/{id}/signatures/owner – landlord signs contract
        [HttpPost("{id:guid}/signatures/owner")]
        [Authorize(Roles = "LandLord")]
        public async Task<IActionResult> UploadOwnerSignature(Guid id, [FromBody] SignatureDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.SignedPdfUrl))
                return BadRequest(new { Message = "Signed PDF URL is required." });

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not identified." });

            var request = new ContractSignatureRequest
            {
                ContractId = id,
                SignedPdfUrl = dto.SignedPdfUrl,
                Role = "Owner",
                UserId = userId
            };

            var result = await _contractService.SignContractAsync(request);
            return Ok(result);
        }

        // POST api/contracts/{id}/admin/approve – admin approves contract → Approved + escrow release
        [HttpPost("{id:guid}/admin/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(Guid id, [FromBody] AdminActionDto? dto)
        {
            var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                              ?? dto?.AdminUserId
                              ?? string.Empty;

            var request = new AdminContractApprovalRequest
            {
                ContractId = id,
                AdminUserId = adminUserId,
                AdminNotes = dto?.Notes,
                IsApproved = true
            };

            var result = await _adminApprovalService.ApproveContractAsync(request);
            if (!result.Success) return BadRequest(new { result.Message });
            return Ok(result);
        }

        // POST api/contracts/{id}/admin/reject – admin rejects contract → Rejected + escrow refund
        [HttpPost("{id:guid}/admin/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] AdminActionDto? dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.Notes))
                return BadRequest(new { Message = "Rejection reason is required." });

            var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                              ?? dto.AdminUserId
                              ?? string.Empty;

            var request = new AdminContractApprovalRequest
            {
                ContractId = id,
                AdminUserId = adminUserId,
                AdminNotes = dto.Notes,
                IsApproved = false
            };

            var result = await _adminApprovalService.RejectContractAsync(request);
            if (!result.Success) return BadRequest(new { result.Message });
            return Ok(result);
        }

        // ── scoped DTOs ───────────────────────────────────────────────────────
        public class SignatureDto
        {
            public string SignedPdfUrl { get; set; } = string.Empty;
        }

        public class AdminActionDto
        {
            public string? AdminUserId { get; set; }
            public string? Notes { get; set; }
        }
    }
}
