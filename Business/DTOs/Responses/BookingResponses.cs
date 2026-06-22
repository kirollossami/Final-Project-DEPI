using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Responses;

public class BookingResponse
{
    public Guid BookingId { get; set; }
    public Guid StudentId { get; set; }
    public BookingType BookingType { get; set; }
    public Guid? BedId { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? HousingUnitId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus BookingStatus { get; set; }
    public bool IsDeleted { get; set; }
    public decimal? CommissionAmount { get; set; }
    public string? ContractId { get; set; }
    public string? ContractPdfUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class BookingIndexedResponse : GenericIndexedResponse<BookingResponse>
{
}
