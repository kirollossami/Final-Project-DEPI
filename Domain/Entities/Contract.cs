using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Contract
    {
        public Guid ContractId { get; set; }
        public Guid BookingId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        
        // Contract Details (for reference, populated from booking)
        public DateTime ReceivingDate { get; set; }
        public DateTime HandoverDate { get; set; }
        public decimal FinalPrice { get; set; }
        public ContractDurationType DurationType { get; set; }
        public int DurationValue { get; set; } // Number of months or years
        
        // Owner Information (for reference)
        public string OwnerFullName { get; set; } = string.Empty;
        public string OwnerNationalId { get; set; } = string.Empty;
        
        // Student Information (for reference)
        public string StudentFullName { get; set; } = string.Empty;
        public string StudentNationalId { get; set; } = string.Empty;
        
        // PDF Storage - Manual Upload Workflow
        public string? OriginalContractPdfPath { get; set; } // Uploaded by admin
        public string? StudentSignedContractPath { get; set; } // Uploaded by student
        public string? LandlordSignedContractPath { get; set; } // Uploaded by landlord
        
        // Signature Status
        public bool IsStudentSigned { get; set; } = false;
        public bool IsLandlordSigned { get; set; } = false;
        public DateTime? StudentSignedAt { get; set; }
        public DateTime? LandlordSignedAt { get; set; }
        
        // Admin Approval
        public bool IsAdminApproved { get; set; } = false;
        public string? AdminUserId { get; set; }
        public DateTime? AdminApprovedAt { get; set; }
        public string? AdminNotes { get; set; }
        
        // Contract Status
        public ContractStatus ContractStatus { get; set; } = ContractStatus.WaitingForUpload;
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual Booking Booking { get; set; } = null!;
        public virtual ICollection<EscrowTransaction> EscrowTransactions { get; set; } = new List<EscrowTransaction>();
    }
}
