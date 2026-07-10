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

public interface IPaymentReceiptRepository : IBaseRepository<PaymentReceipt>
{
    Task<PaymentReceipt?> GetByPaymentIdAsync(Guid paymentId);
    Task<IEnumerable<PaymentReceipt>> GetAllByPaymentIdAsync(Guid paymentId);
    Task<IEnumerable<PaymentReceipt>> GetByUserIdAsync(string userId);
    Task<IEnumerable<PaymentReceipt>> GetByEscrowIdAsync(Guid escrowId);
}

public class PaymentReceiptRepository : BaseRepository<PaymentReceipt>, IPaymentReceiptRepository
{
    public PaymentReceiptRepository(StudentHousingDBContext context) : base(context)
    {
    }

    // Override GetAsync to always include the Payment navigation property
    // so BookingId and PaymentStatus are available for MapToResponse
    public override IQueryable<PaymentReceipt> GetAll(bool asNoTracking = false)
    {
        var query = entities.Include(pr => pr.Payment).AsQueryable();
        return asNoTracking ? query.AsNoTracking() : query;
    }

    public new async Task<PaymentReceipt?> GetAsync(object id)
    {
        if (id is Guid guid)
        {
            return await entities
                .Include(pr => pr.Payment)
                .FirstOrDefaultAsync(pr => pr.ReceiptId == guid);
        }
        return await entities.FindAsync(id);
    }

    public async Task<PaymentReceipt?> GetByPaymentIdAsync(Guid paymentId)
    {
        return await entities
            .Include(pr => pr.Payment)
            .Include(pr => pr.EscrowTransaction)
            .FirstOrDefaultAsync(pr => pr.PaymentId == paymentId);
    }

    public async Task<IEnumerable<PaymentReceipt>> GetAllByPaymentIdAsync(Guid paymentId)
    {
        return await entities
            .Include(pr => pr.Payment)
            .Where(pr => pr.PaymentId == paymentId)
            .OrderByDescending(pr => pr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PaymentReceipt>> GetByUserIdAsync(string userId)
    {
        return await entities
            .Include(pr => pr.Payment)
            .Where(pr => pr.IssuedToUserId == userId)
            .OrderByDescending(pr => pr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PaymentReceipt>> GetByEscrowIdAsync(Guid escrowId)
    {
        return await entities
            .Include(pr => pr.Payment)
            .Include(pr => pr.EscrowTransaction)
            .Where(pr => pr.EscrowId == escrowId)
            .ToListAsync();
    }
}
