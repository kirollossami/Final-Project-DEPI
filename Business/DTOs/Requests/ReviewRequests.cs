using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Requests;

/// <summary>
/// Request model for creating a new review
/// </summary>
public class ReviewCreateRequest
{
    public Guid StudentId { get; set; }
    public Guid HousingUnitId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

/// <summary>
/// Request model for updating an existing review
/// </summary>
public class ReviewUpdateRequest
{
    public Guid ReviewId { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }
}

/// <summary>
/// Request model for filtering/searching reviews
/// </summary>
public class ReviewFilterRequest
{
    public Guid? StudentId { get; set; }
    public Guid? HousingUnitId { get; set; }
    public int? MinRating { get; set; }
    public int? MaxRating { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
