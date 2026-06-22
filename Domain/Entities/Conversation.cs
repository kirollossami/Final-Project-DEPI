namespace Domain.Entities;

public class Conversation
{
    public Guid ConversationId { get; set; }
    public Guid BookingId { get; set; }
    public string StudentUserId { get; set; }
    public string LandLordUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual Booking? Booking { get; set; }
    public virtual ICollection<Message>? Messages { get; set; }
}
