namespace Domain.Entities;

public class Message
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public string SenderId { get; set; }
    public string Content { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }

    public virtual Conversation? Conversation { get; set; }
}
