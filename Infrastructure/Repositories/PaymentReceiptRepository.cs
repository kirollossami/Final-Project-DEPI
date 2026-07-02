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
    Task<IEnumerable<PaymentReceipt>> GetByUserIdAsync(string userId);
    Task<IEnumerable<PaymentReceipt>> GetByEscrowIdAsync(Guid escrowId);
}

public class PaymentReceiptRepository : BaseRepository<PaymentReceipt>, IPaymentReceiptRepository
{
    public PaymentReceiptRepository(StudentHousingDBContext context) : base(context)
    {
    }

    public async Task<PaymentReceipt?> GetByPaymentIdAsync(Guid paymentId)
    {
        return await entities
            .Include(pr => pr.Payment)
            .Include(pr => pr.EscrowTransaction)
            .FirstOrDefaultAsync(pr => pr.PaymentId == paymentId);
    }

    public async Task<IEnumerable<PaymentReceipt>> GetByUserIdAsync(string userId)
    {
        return await entities
            .Where(pr => pr.IssuedToUserId == userId)
            .OrderByDescending(pr => pr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PaymentReceipt>> GetByEscrowIdAsync(Guid escrowId)
    {
        return await entities
            .Include(pr => pr.EscrowTransaction)
            .Where(pr => pr.EscrowId == escrowId)
            .ToListAsync();
    }
}
