namespace Domain.Entities;

/// <summary>
/// PaymentHistory tracks all payment-related events and state changes for audit and compliance purposes.
/// This provides a complete audit trail for financial transactions.
/// </summary>
public class PaymentHistory
{
    public Guid HistoryId { get; set; }

    // References
    public Guid PaymentId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? EscrowTransactionId { get; set; }
    public string UserId { get; set; } = string.Empty;

    // Event Details
    public string EventType { get; set; } = string.Empty; // PaymentInitiated, PaymentCompleted, PaymentFailed, EscrowHeld, EscrowReleased, etc.
    public string Description { get; set; } = string.Empty;

    // Financial Details
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";

    // Status Information
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;

    // Additional Data (JSON for flexibility)
    public string? MetadataJson { get; set; }

    // Actor Information
    public string? ActorUserId { get; set; }
    public string? ActorRole { get; set; }

    // System Information
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Payment? Payment { get; set; }
    public virtual Booking? Booking { get; set; }
    public virtual EscrowTransaction? EscrowTransaction { get; set; }
    public virtual User? User { get; set; }
}
