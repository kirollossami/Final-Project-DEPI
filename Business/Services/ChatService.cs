using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class ChatService : IChatService
{
    private readonly IBaseRepository<Conversation> _conversationRepository;
    private readonly IBaseRepository<Message> _messageRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IHousingUnitRepository _housingUnitRepository;
    private readonly UserManager<User> _userManager;
    private readonly INotificationService _notificationService;

    public ChatService(
        IBaseRepository<Conversation> conversationRepository,
        IBaseRepository<Message> messageRepository,
        IBookingRepository bookingRepository,
        IHousingUnitRepository housingUnitRepository,
        UserManager<User> userManager,
        INotificationService notificationService)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _bookingRepository = bookingRepository;
        _housingUnitRepository = housingUnitRepository;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    public async Task<ConversationResponse> GetOrCreateConversationAsync(Guid bookingId, string userId)
    {
        var conversation = await _conversationRepository.GetAll()
            .FirstOrDefaultAsync(c => c.BookingId == bookingId);

        if (conversation != null)
        {
            return MapConversation(conversation);
        }

        var booking = await _bookingRepository.GetAll()
            .Include(b => b.Student)
            .Include(b => b.HousingUnit)
                .ThenInclude(h => h.LandLord)
            .Include(b => b.Room)
                .ThenInclude(r => r.HousingUnit)
                    .ThenInclude(h => h.LandLord)
            .Include(b => b.Bed)
                .ThenInclude(bd => bd.Room)
                    .ThenInclude(r => r.HousingUnit)
                        .ThenInclude(h => h.LandLord)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        if (booking?.Student?.UserId == null)
        {
            throw new InvalidOperationException("Booking or associated student not found.");
        }

        var landLord = booking.HousingUnit?.LandLord
            ?? booking.Room?.HousingUnit?.LandLord
            ?? booking.Bed?.Room?.HousingUnit?.LandLord;

        if (landLord?.UserId == null)
        {
            throw new InvalidOperationException("Booking or associated landlord not found.");
        }

        var housingUnitId = booking.HousingUnitId
            ?? booking.Room?.HousingUnitId
            ?? booking.Bed?.Room?.HousingUnitId;

        var preBookingConversation = await _conversationRepository.GetAll()
            .FirstOrDefaultAsync(c =>
                c.HousingUnitId == housingUnitId &&
                c.StudentUserId == booking.Student.UserId &&
                c.BookingId == null);

        if (preBookingConversation != null)
        {
            preBookingConversation.BookingId = bookingId;
            await _conversationRepository.Update(preBookingConversation);
            await _conversationRepository.CommitAsync();
            return MapConversation(preBookingConversation);
        }

        conversation = new Conversation
        {
            ConversationId = Guid.NewGuid(),
            BookingId = bookingId,
            HousingUnitId = housingUnitId ?? Guid.Empty,
            StudentUserId = booking.Student.UserId,
            LandLordUserId = landLord.UserId,
            CreatedAt = DateTime.UtcNow
        };

        await _conversationRepository.Insert(conversation);
        await _conversationRepository.CommitAsync();

        return MapConversation(conversation);
    }

    public async Task<ConversationResponse> InitiatePreBookingConversationAsync(Guid housingUnitId, string userId)
    {
        var existing = await _conversationRepository.GetAll()
            .FirstOrDefaultAsync(c => c.HousingUnitId == housingUnitId && c.StudentUserId == userId && c.BookingId == null);

        if (existing != null)
        {
            return MapConversation(existing);
        }

        var housingUnit = await _housingUnitRepository.GetAll()
            .Include(h => h.LandLord)
            .FirstOrDefaultAsync(h => h.HousingUnitId == housingUnitId);

        if (housingUnit?.LandLord?.UserId == null)
        {
            throw new InvalidOperationException("Housing unit or associated landlord not found.");
        }

        var conversation = new Conversation
        {
            ConversationId = Guid.NewGuid(),
            BookingId = null,
            HousingUnitId = housingUnitId,
            StudentUserId = userId,
            LandLordUserId = housingUnit.LandLord.UserId,
            CreatedAt = DateTime.UtcNow
        };

        await _conversationRepository.Insert(conversation);
        await _conversationRepository.CommitAsync();

        return MapConversation(conversation);
    }

    public async Task<ConversationResponse?> GetConversationByIdAsync(Guid conversationId, string userId)
    {
        var conversation = await _conversationRepository.GetAsync(conversationId);
        if (conversation == null) return null;

        if (conversation.StudentUserId != userId && conversation.LandLordUserId != userId)
        {
            throw new UnauthorizedAccessException("User is not a participant of this conversation.");
        }

        return MapConversation(conversation);
    }

    public async Task<PagedMessagesResponse> GetMessagesAsync(Guid conversationId, string userId, int page = 1, int pageSize = 20)
    {
        if (!await IsParticipantAsync(conversationId, userId))
        {
            throw new UnauthorizedAccessException("User is not a participant of this conversation.");
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _messageRepository.GetAll()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt);

        var totalCount = await query.CountAsync();
        var messages = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedMessagesResponse
        {
            Messages = messages.Select(m => new MessageResponse
            {
                MessageId = m.MessageId,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                Content = m.Content,
                SentAt = m.SentAt,
                IsRead = m.IsRead
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            HasMore = (page * pageSize) < totalCount
        };
    }

    public async Task<MessageResponse> SaveMessageAsync(Guid conversationId, string senderId, string content)
    {
        if (!await IsParticipantAsync(conversationId, senderId))
        {
            throw new UnauthorizedAccessException("User is not a participant of this conversation.");
        }

        var message = new Message
        {
            MessageId = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        await _messageRepository.Insert(message);

        var conversation = await _conversationRepository.GetAsync(conversationId);
        if (conversation != null)
        {
            conversation.UpdatedAt = DateTime.UtcNow;
        }

        await _messageRepository.CommitAsync();

        try
        {
            var convo = await _conversationRepository.GetAsync(conversationId);
            if (convo != null)
            {
                var recipientId = convo.StudentUserId == senderId
                    ? convo.LandLordUserId
                    : convo.StudentUserId;

                var sender = await _userManager.FindByIdAsync(senderId);
                var senderName = sender?.UserName ?? sender?.Email ?? "Someone";

                await _notificationService.SendRealTimeNotificationAsync(
                    recipientId,
                    $"You have a new message from {senderName}.",
                    NotificationTypes.NewMessageReceived);
            }
        }
        catch { }

        return new MessageResponse
        {
            MessageId = message.MessageId,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            Content = message.Content,
            SentAt = message.SentAt,
            IsRead = message.IsRead
        };
    }

    public async Task MarkAsReadAsync(Guid conversationId, string userId)
    {
        if (!await IsParticipantAsync(conversationId, userId))
        {
            throw new UnauthorizedAccessException("User is not a participant of this conversation.");
        }

        var unreadMessages = await _messageRepository.GetAll()
            .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead)
            .ToListAsync();

        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
        }

        if (unreadMessages.Count > 0)
        {
            await _messageRepository.CommitAsync();
        }
    }

    public async Task<List<ConversationResponse>> GetUserConversationsAsync(string userId)
    {
        var conversationIds = await _conversationRepository.GetAll()
            .Where(c => c.StudentUserId == userId || c.LandLordUserId == userId)
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Select(c => c.ConversationId)
            .ToListAsync();

        if (conversationIds.Count == 0)
            return new List<ConversationResponse>();

        var conversations = await _conversationRepository.GetAll()
            .Where(c => conversationIds.Contains(c.ConversationId))
            .ToListAsync();

        var ordered = conversationIds
            .Select(id => conversations.First(c => c.ConversationId == id))
            .ToList();

        var userIds = ordered
            .SelectMany(c => new[] { c.StudentUserId, c.LandLordUserId })
            .Distinct()
            .ToList();

        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Email ?? "Unknown");

        var lastMessages = await _messageRepository.GetAll()
            .Where(m => conversationIds.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => g.OrderByDescending(m => m.SentAt).First())
            .ToDictionaryAsync(m => m.ConversationId);

        return ordered.Select(c =>
        {
            var last = lastMessages.GetValueOrDefault(c.ConversationId);
            return new ConversationResponse
            {
                ConversationId = c.ConversationId,
                BookingId = c.BookingId,
                HousingUnitId = c.HousingUnitId,
                StudentUserId = c.StudentUserId,
                LandLordUserId = c.LandLordUserId,
                StudentName = users.GetValueOrDefault(c.StudentUserId),
                LandlordName = users.GetValueOrDefault(c.LandLordUserId),
                LastMessage = last?.Content,
                LastMessageAt = last?.SentAt,
                CreatedAt = c.CreatedAt
            };
        }).ToList();
    }

    public async Task<bool> IsParticipantAsync(Guid conversationId, string userId)
    {
        var conversation = await _conversationRepository.GetAsync(conversationId);
        if (conversation == null) return false;

        return conversation.StudentUserId == userId || conversation.LandLordUserId == userId;
    }

    private static ConversationResponse MapConversation(Conversation conversation)
    {
        return new ConversationResponse
        {
            ConversationId = conversation.ConversationId,
            BookingId = conversation.BookingId,
            HousingUnitId = conversation.HousingUnitId,
            StudentUserId = conversation.StudentUserId,
            LandLordUserId = conversation.LandLordUserId,
            CreatedAt = conversation.CreatedAt
        };
    }
}
