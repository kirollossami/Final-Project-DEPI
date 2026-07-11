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

        // Paymob Intention API: id = "pi_test_xxx..." — stored in both fields for query flexibility
        public string PaymobOrderId { get; set; } = string.Empty;       // intention id (pi_test_...)
        public string PaymobIntentionId { get; set; } = string.Empty;   // intention id (same value)

        // After payment completes, Paymob sends back a numeric transaction ID and a
        // separate numeric order ID. These are different from the intention ID.
        // GET /callback?id=493478206&order=563150958
        //   id    → PaymobTransactionId (numeric, assigned when user pays)
        //   order → PaymobNumericOrderId (numeric order assigned by Paymob at order creation)
        public string? PaymobTransactionId { get; set; }    // numeric, e.g. "493478206"
        public string? PaymobNumericOrderId { get; set; }   // numeric, e.g. "563150958" — NEW FIELD
        
        // Payment Details
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public PaymentGatewayStatus GatewayStatus { get; set; }
        
        // Paymob Response Data
        public string? PaymentToken { get; set; }
        public string? PaymentUrl { get; set; }
        public string? RawResponse { get; set; }
        public string? ClientSecret { get; set; }
        
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
