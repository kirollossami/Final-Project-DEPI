using Business.DTOs.Responses;
using Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace StudentHousingAPI.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task JoinConversation(string conversationId)
    {
        if (!Guid.TryParse(conversationId, out var convId))
        {
            await Clients.Caller.SendAsync("Error", "Invalid conversation ID.");
            return;
        }

        var userId = Context.UserIdentifier;
        if (userId == null) return;

        var isParticipant = await _chatService.IsParticipantAsync(convId, userId);
        if (!isParticipant)
        {
            await Clients.Caller.SendAsync("Error", "You are not a participant of this conversation.");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        await Clients.Caller.SendAsync("Joined", conversationId);
    }

    public async Task SendMessage(string conversationId, string content)
    {
        if (!Guid.TryParse(conversationId, out var convId))
        {
            await Clients.Caller.SendAsync("Error", "Invalid conversation ID.");
            return;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            await Clients.Caller.SendAsync("Error", "Message content cannot be empty.");
            return;
        }

        var userId = Context.UserIdentifier;
        if (userId == null) return;

        try
        {
            var message = await _chatService.SaveMessageAsync(convId, userId, content);
            await Clients.Group(conversationId).SendAsync("ReceiveMessage", message);
        }
        catch (UnauthorizedAccessException)
        {
            await Clients.Caller.SendAsync("Error", "You are not a participant of this conversation.");
        }
    }

    public async Task MarkAsRead(string conversationId)
    {
        if (!Guid.TryParse(conversationId, out var convId))
        {
            await Clients.Caller.SendAsync("Error", "Invalid conversation ID.");
            return;
        }

        var userId = Context.UserIdentifier;
        if (userId == null) return;

        try
        {
            await _chatService.MarkAsReadAsync(convId, userId);
            await Clients.Group(conversationId).SendAsync("MessagesRead", conversationId, userId);
        }
        catch (UnauthorizedAccessException)
        {
            await Clients.Caller.SendAsync("Error", "You are not a participant of this conversation.");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
