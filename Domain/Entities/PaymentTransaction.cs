using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class PaymentTransaction
    {
        public Guid TransactionId { get; set; }
        public Guid PaymentId { get; set; }
        public string PaymobOrderId { get; set; } = string.Empty;
        public string PaymobIntentionId { get; set; } = string.Empty;
        public string? PaymobTransactionId { get; set; }
        
        // Payment Details
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public PaymentGatewayStatus GatewayStatus { get; set; }
        
        // Paymob Response Data
        public string? PaymentToken { get; set; }
        public string? PaymentUrl { get; set; }
        public string? RawResponse { get; set; }
        
        // Callback Data
        public string? CallbackSuccess { get; set; }
        public string? CallbackPending { get; set; }
        public string? CallbackFailed { get; set; }
        public DateTime? CallbackProcessedAt { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public virtual Payment Payment { get; set; } = null!;
    }
}
