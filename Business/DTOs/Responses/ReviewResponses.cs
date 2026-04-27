using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Responses;

public class ReviewResponse
{
    public Guid ReviewId { get; set; }
    public Guid StudentId { get; set; }
    public Guid HousingUnitId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime ReviewDate { get; set; }
}

public class ReviewIndexedResponse : GenericIndexedResponse<ReviewResponse>
{
}
