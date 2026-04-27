using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface INotificationService
{
    Task<NotificationResponse?> GetNotificationByIdAsync(Guid notificationId);
    Task<NotificationIndexedResponse> GetNotificationsAsync(NotificationFilterRequest filter);
    Task<NotificationResponse?> CreateNotificationAsync(NotificationCreateRequest request);
    Task<bool> MarkAsReadAsync(Guid notificationId);
}
