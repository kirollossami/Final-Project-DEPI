namespace Business.DTOs.Requests;

public class SendMessageRequest
{
    public string Content { get; set; }
}

public class InitiateConversationRequest
{
    public Guid HousingUnitId { get; set; }
}
