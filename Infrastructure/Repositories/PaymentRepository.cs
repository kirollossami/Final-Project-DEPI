using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public interface IPaymentRepository : IBaseRepository<Payment>
{
    Task<Payment?> GetByBookingIdAsync(Guid bookingId);
    Task<IEnumerable<Payment>> GetByUserIdAsync(string userId);
}

public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(StudentHousingDBContext context) : base(context)
    {
    }

    public async Task<Payment?> GetByBookingIdAsync(Guid bookingId)
    {
        return await entities
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.BookingId == bookingId);
    }

    public async Task<IEnumerable<Payment>> GetByUserIdAsync(string userId)
    {
        return await entities
            .Include(p => p.Booking)
            .ThenInclude(b => b.Student)
            .Where(p => p.Booking.Student.UserId == userId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
    }
}
