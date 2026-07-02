using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class EscrowTransaction
    {
        public Guid EscrowId { get; set; }
        public Guid PaymentId { get; set; }
        public Guid ContractId { get; set; }
        
        // Escrow Details
        public decimal HeldAmount { get; set; }
        public string Currency { get; set; } = "EGP";
        public EscrowStatus Status { get; set; }
        
        // Release Details
        public DateTime? ReleasedAt { get; set; }
        public string? ReleasedByUserId { get; set; }
        public string? ReleaseTransactionId { get; set; }
        public string? ReleaseNotes { get; set; }
        
        // Refund Details (if applicable)
        public DateTime? RefundedAt { get; set; }
        public string? RefundTransactionId { get; set; }
        public string? RefundReason { get; set; }
        
        // Payout Details to Owner
        public string? OwnerPayoutTransactionId { get; set; }
        public decimal? OwnerPayoutAmount { get; set; }
        public DateTime? OwnerPayoutAt { get; set; }
        
        // Platform Fee
        public decimal PlatformFee { get; set; }
        public decimal PlatformFeePercentage { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual Payment Payment { get; set; } = null!;
        public virtual Contract Contract { get; set; } = null!;
        public virtual ICollection<PaymentReceipt> PaymentReceipts { get; set; } = new List<PaymentReceipt>();
    }
}
