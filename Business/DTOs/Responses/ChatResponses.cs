namespace Business.DTOs.Responses;

public class ConversationResponse
{
    public Guid ConversationId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? HousingUnitId { get; set; }
    public string StudentUserId { get; set; }
    public string LandLordUserId { get; set; }
    public string? StudentName { get; set; }
    public string? LandlordName { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
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
