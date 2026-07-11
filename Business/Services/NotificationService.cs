using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationDispatcher _dispatcher;
    private readonly UserManager<User> _userManager;

    public NotificationService(
        INotificationRepository notificationRepository,
        INotificationDispatcher dispatcher,
        UserManager<User> userManager)
    {
        _notificationRepository = notificationRepository;
        _dispatcher = dispatcher;
        _userManager = userManager;
    }

    public async Task<NotificationResponse?> GetNotificationByIdAsync(Guid notificationId)
    {
        var notification = await _notificationRepository.GetAsync(notificationId);
        if (notification == null) return null;

        return new NotificationResponse
        {
            NotificationId = notification.NotificationId,
            UserId = notification.UserId,
            Message = notification.Message,
            Type = notification.Type,
            IsSeen = notification.IsSeen,
            CreatedAt = notification.CreatedAt
        };
    }

    public async Task<NotificationIndexedResponse> GetNotificationsAsync(NotificationFilterRequest filter)
    {
        var query = _notificationRepository.GetAll().AsQueryable();

        if (filter.UserId != null)
            query = query.Where(n => n.UserId == filter.UserId);

        if (filter.IsSeen.HasValue)
            query = query.Where(n => n.IsSeen == filter.IsSeen.Value);

        if (!string.IsNullOrEmpty(filter.Type))
            query = query.Where(n => n.Type == filter.Type);

        var totalCount = await query.CountAsync();
        var notifications = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new NotificationIndexedResponse
        {
            Records = notifications.Select(n => new NotificationResponse
            {
                NotificationId = n.NotificationId,
                UserId = n.UserId,
                Message = n.Message,
                Type = n.Type,
                IsSeen = n.IsSeen,
                CreatedAt = n.CreatedAt
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
        };
    }

    public async Task<NotificationResponse?> CreateNotificationAsync(NotificationCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new ArgumentException("UserId cannot be null or empty", nameof(request));

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            throw new InvalidOperationException($"User with Id '{request.UserId}' does not exist in AspNetUsers");

        var notification = new Domain.Entities.Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = request.UserId,
            Message = request.Message,
            Type = request.Type,
            IsSeen = false,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository.Insert(notification);
        await _notificationRepository.CommitAsync();

        var response = new NotificationResponse
        {
            NotificationId = notification.NotificationId,
            UserId = notification.UserId,
            Message = notification.Message,
            Type = notification.Type,
            IsSeen = notification.IsSeen,
            CreatedAt = notification.CreatedAt
        };

        await _dispatcher.DispatchToUserAsync(request.UserId, response);

        return response;
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _notificationRepository.GetAsync(notificationId);
        if (notification == null) return false;

        notification.IsSeen = true;
        await _notificationRepository.Update(notification);
        await _notificationRepository.CommitAsync();

        return true;
    }

    public async Task SendRealTimeNotificationAsync(string userId, string message, string type)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException($"User with Id '{userId}' does not exist in AspNetUsers");

        var notification = new Domain.Entities.Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Message = message,
            Type = type,
            IsSeen = false,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository.Insert(notification);
        await _notificationRepository.CommitAsync();

        await _dispatcher.DispatchToUserAsync(userId, new
        {
            notification.NotificationId,
            notification.UserId,
            notification.Message,
            notification.Type,
            notification.IsSeen,
            notification.CreatedAt
        });
    }

    public async Task SendNotificationToRoleAsync(string role, string message, string type)
    {
        var users = await _userManager.GetUsersInRoleAsync(role);
        foreach (var user in users)
        {
            var notification = new Domain.Entities.Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = user.Id,
                Message = message,
                Type = type,
                IsSeen = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.Insert(notification);
            await _notificationRepository.CommitAsync();

            await _dispatcher.DispatchToUserAsync(user.Id, new
            {
                notification.NotificationId,
                notification.UserId,
                notification.Message,
                notification.Type,
                notification.IsSeen,
                notification.CreatedAt
            });
        }
    }
}
