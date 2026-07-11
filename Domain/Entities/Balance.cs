using System;

namespace Domain.Entities
{
    /// <summary>
    /// Represents the balance for admin and landlord accounts
    /// Tracks funds held by the platform and payouts to landlords
    /// </summary>
    public class Balance
    {
        public Guid BalanceId { get; set; }
        
        /// <summary>
        /// User ID (Admin or Landlord)
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// User role (Admin or LandLord)
        /// </summary>
        public string UserRole { get; set; } = string.Empty;
        
        /// <summary>
        /// Current available balance
        /// </summary>
        public decimal AvailableBalance { get; set; } = 0;
        
        /// <summary>
        /// Total amount received
        /// </summary>
        public decimal TotalReceived { get; set; } = 0;
        
        /// <summary>
        /// Total amount paid out
        /// </summary>
        public decimal TotalPaidOut { get; set; } = 0;
        
        /// <summary>
        /// Currency (default: EGP)
        /// </summary>
        public string Currency { get; set; } = "EGP";
        
        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Created timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
