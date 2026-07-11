using Business.Interfaces;

namespace Business.Services;

public class NullNotificationDispatcher : INotificationDispatcher
{
    public Task DispatchToUserAsync(string userId, object payload) => Task.CompletedTask;
    public Task DispatchToRoleAsync(string role, object payload) => Task.CompletedTask;
}
