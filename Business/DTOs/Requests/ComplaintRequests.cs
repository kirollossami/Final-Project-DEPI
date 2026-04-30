using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Requests;

/// <summary>
/// Request model for creating a new complaint
/// </summary>
public class ComplaintCreateRequest
{
    public Guid StudentId { get; set; }
    public Guid LandLordId { get; set; }
    public string Description { get; set; }
}

/// <summary>
/// Request model for updating an existing complaint
/// </summary>
public class ComplaintUpdateRequest
{
    public Guid ComplaintId { get; set; }
    public string? Description { get; set; }
    public ComplaintStatus? Status { get; set; }
}

/// <summary>
/// Request model for filtering/searching complaints
/// </summary>
public class ComplaintFilterRequest
{
    public Guid? StudentId { get; set; }
    public Guid? LandLordId { get; set; }
    public ComplaintStatus? Status { get; set; }
    public DateTime? CreatedDateFrom { get; set; }
    public DateTime? CreatedDateTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
