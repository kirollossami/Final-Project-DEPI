using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class PaymentReceipt
    {
        public Guid ReceiptId { get; set; }
        public Guid PaymentId { get; set; }
        public Guid EscrowId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        
        // Receipt Details
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public ReceiptType Type { get; set; }
        
        // Party Information
        public string IssuedToUserId { get; set; } = string.Empty;
        public string IssuedToRole { get; set; } = string.Empty; // Student, Owner, Admin
        public string IssuedToName { get; set; } = string.Empty;
        
        // Transaction Reference
        public string TransactionReference { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        
        // Receipt Content (JSON for flexibility)
        public string ReceiptData { get; set; } = string.Empty; // JSON with all receipt details
        
        // PDF Storage
        public string ReceiptPdfUrl { get; set; } = string.Empty;
        
        // Status
        public bool IsEmailSent { get; set; } = false;
        public DateTime? EmailSentAt { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Payment Payment { get; set; } = null!;
        public virtual EscrowTransaction EscrowTransaction { get; set; } = null!;
    }
}
