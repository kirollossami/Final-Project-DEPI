using Business.Interfaces;
using Microsoft.AspNetCore.SignalR;
using StudentHousingAPI.Hubs;

namespace StudentHousingAPI.Services;

public class SignalRNotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationDispatcher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task DispatchToUserAsync(string userId, object payload)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;
        await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", payload);
    }

    public async Task DispatchToRoleAsync(string role, object payload)
    {
        await _hubContext.Clients.Group($"role_{role}").SendAsync("ReceiveNotification", payload);
    }
}
