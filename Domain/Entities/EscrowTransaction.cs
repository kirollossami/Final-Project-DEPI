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
        public Guid BookingId { get; set; }
        public Guid StudentId { get; set; }
        public Guid LandlordId { get; set; }
        public Guid? PaymentId { get; set; } // Optional: linked to payment if exists
        public Guid? ContractId { get; set; } // Optional: linked to contract if exists
        
        // Escrow Details
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public EscrowStatus Status { get; set; }
        public string TransactionType { get; set; } = "Payment"; // Payment, Release, Refund
        public string PaymentReference { get; set; } = string.Empty;
        
        // Release Details (when approved by admin)
        public DateTime? ReleasedAt { get; set; }
        public string? ReleasedByUserId { get; set; }
        public string? ReleaseTransactionId { get; set; }
        public string? ReleaseNotes { get; set; }
        
        // Refund Details (when rejected by admin)
        public DateTime? RefundedAt { get; set; }
        public string? RefundTransactionId { get; set; }
        public string? RefundReason { get; set; }
        
        // Payout Details to Landlord
        public string? LandlordPayoutTransactionId { get; set; }
        public decimal? LandlordPayoutAmount { get; set; }
        public DateTime? LandlordPayoutAt { get; set; }
        
        // Platform Fee
        public decimal PlatformFee { get; set; }
        public decimal PlatformFeePercentage { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual Booking Booking { get; set; } = null!;
        public virtual Payment? Payment { get; set; }
        public virtual Contract? Contract { get; set; }
        public virtual ICollection<PaymentReceipt> PaymentReceipts { get; set; } = new List<PaymentReceipt>();
    }
}
