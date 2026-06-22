namespace Business.DTOs.Responses;

public class ConversationResponse
{
    public Guid ConversationId { get; set; }
    public Guid BookingId { get; set; }
    public string StudentUserId { get; set; }
    public string LandLordUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MessageResponse
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public string SenderId { get; set; }
    public string Content { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
}

public class PagedMessagesResponse
{
    public List<MessageResponse> Messages { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}
