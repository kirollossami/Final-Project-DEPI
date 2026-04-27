using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Requests;

/// <summary>
/// Request model for creating a new payment
/// </summary>
public class PaymentCreateRequest
{
    public Guid BookingId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
}

/// <summary>
/// Request model for updating an existing payment
/// </summary>
public class PaymentUpdateRequest
{
    public Guid PaymentId { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public string? TransactionId { get; set; }
}

/// <summary>
/// Request model for filtering/searching payments
/// </summary>
public class PaymentFilterRequest
{
    public Guid? BookingId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public DateTime? PaymentDateFrom { get; set; }
    public DateTime? PaymentDateTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
