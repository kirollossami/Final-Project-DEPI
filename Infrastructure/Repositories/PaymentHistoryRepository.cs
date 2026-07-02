using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository for PaymentHistory entity - tracks audit logs of all payment transactions
/// </summary>
public interface IPaymentHistoryRepository : IBaseRepository<PaymentHistory>
{
    Task<IEnumerable<PaymentHistory>> GetPaymentHistoryByUserAsync(string userId);
    Task<IEnumerable<PaymentHistory>> GetPaymentHistoryByBookingAsync(Guid bookingId);
    Task<IEnumerable<PaymentHistory>> GetPaymentHistoryByPaymentAsync(Guid paymentId);
    Task<IEnumerable<PaymentHistory>> GetPaymentHistoryByDateRangeAsync(DateTime startDate, DateTime endDate);
}

public class PaymentHistoryRepository : BaseRepository<PaymentHistory>, IPaymentHistoryRepository
{
    public PaymentHistoryRepository(StudentHousingDBContext context) : base(context)
    {
    }

    public async Task<IEnumerable<PaymentHistory>> GetPaymentHistoryByUserAsync(string userId)
    {
        return await entities
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PaymentHistory>> GetPaymentHistoryByBookingAsync(Guid bookingId)
    {
        return await entities
            .Where(ph => ph.BookingId == bookingId)
            .OrderByDescending(ph => ph.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PaymentHistory>> GetPaymentHistoryByPaymentAsync(Guid paymentId)
    {
        return await entities
            .Where(ph => ph.PaymentId == paymentId)
            .OrderByDescending(ph => ph.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PaymentHistory>> GetPaymentHistoryByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await entities
            .Where(ph => ph.CreatedAt >= startDate && ph.CreatedAt <= endDate)
            .OrderByDescending(ph => ph.CreatedAt)
            .ToListAsync();
    }
}
