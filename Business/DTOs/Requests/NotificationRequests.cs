using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Requests;

/// <summary>
/// Request model for creating a new notification
/// </summary>
public class NotificationCreateRequest
{
    public string UserId { get; set; }
    public string Message { get; set; }
    public string Type { get; set; }
}

/// <summary>
/// Request model for updating a notification
/// </summary>
public class NotificationUpdateRequest
{
    public Guid NotificationId { get; set; }
    public bool? IsSeen { get; set; }
}

/// <summary>
/// Request model for filtering/searching notifications
/// </summary>
public class NotificationFilterRequest
{
    public string? UserId { get; set; }
    public string? Type { get; set; }
    public bool? IsSeen { get; set; }
    public DateTime? CreatedDateFrom { get; set; }
    public DateTime? CreatedDateTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
