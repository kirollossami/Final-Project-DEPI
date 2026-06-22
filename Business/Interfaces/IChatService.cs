using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IChatService
{
    Task<ConversationResponse> GetOrCreateConversationAsync(Guid bookingId, string userId);
    Task<ConversationResponse?> GetConversationByIdAsync(Guid conversationId, string userId);
    Task<PagedMessagesResponse> GetMessagesAsync(Guid conversationId, string userId, int page = 1, int pageSize = 20);
    Task<MessageResponse> SaveMessageAsync(Guid conversationId, string senderId, string content);
    Task MarkAsReadAsync(Guid conversationId, string userId);
    Task<bool> IsParticipantAsync(Guid conversationId, string userId);
}
