using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Responses;

public class ComplaintResponse
{
    public Guid ComplaintId { get; set; }
    public Guid StudentId { get; set; }
    public Guid LandLordId { get; set; }
    public string Description { get; set; }
    public ComplaintStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class ComplaintIndexedResponse : GenericIndexedResponse<ComplaintResponse>
{
}
