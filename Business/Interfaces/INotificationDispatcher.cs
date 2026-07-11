namespace Business.Interfaces;

public interface INotificationDispatcher
{
    Task DispatchToUserAsync(string userId, object payload);
    Task DispatchToRoleAsync(string role, object payload);
}
