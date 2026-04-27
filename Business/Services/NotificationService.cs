using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
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
        {
            query = query.Where(n => n.UserId == filter.UserId);
        }

        if (filter.IsSeen.HasValue)
        {
            query = query.Where(n => n.IsSeen == filter.IsSeen.Value);
        }

        if (!string.IsNullOrEmpty(filter.Type))
        {
            query = query.Where(n => n.Type == filter.Type);
        }

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

    public async Task<bool> MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _notificationRepository.GetAsync(notificationId);
        if (notification == null) return false;

        notification.IsSeen = true;
        _notificationRepository.Update(notification);
        await _notificationRepository.CommitAsync();

        return true;
    }
}
