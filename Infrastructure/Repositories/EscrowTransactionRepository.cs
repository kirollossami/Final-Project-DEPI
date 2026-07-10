using Domain.Entities;
using Domain.Enums;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public interface IEscrowTransactionRepository : IBaseRepository<EscrowTransaction>
{
    Task<EscrowTransaction?> GetByPaymentIdAsync(Guid paymentId);
    Task<EscrowTransaction?> GetByContractIdAsync(Guid contractId);
    Task<EscrowTransaction?> GetByBookingIdAsync(Guid bookingId);
    Task<IEnumerable<EscrowTransaction>> GetHoldingEscrowsAsync();
    Task<IEnumerable<EscrowTransaction>> GetPendingReleaseAsync();
}

public class EscrowTransactionRepository : BaseRepository<EscrowTransaction>, IEscrowTransactionRepository
{
    public EscrowTransactionRepository(StudentHousingDBContext context) : base(context)
    {
    }

    public async Task<EscrowTransaction?> GetByPaymentIdAsync(Guid paymentId)
    {
        return await entities
            .AsNoTracking()
            .Include(e => e.Payment)
            .Include(e => e.Contract)
            .FirstOrDefaultAsync(e => e.PaymentId == paymentId);
    }

    public async Task<EscrowTransaction?> GetByContractIdAsync(Guid contractId)
    {
        return await entities
            .AsNoTracking()
            .Include(e => e.Contract)
            .Include(e => e.Payment)
            .FirstOrDefaultAsync(e => e.ContractId == contractId);
    }

    public async Task<EscrowTransaction?> GetByBookingIdAsync(Guid bookingId)
    {
        return await entities
            .AsNoTracking()
            .Include(e => e.Contract)
            .Include(e => e.Payment)
            .FirstOrDefaultAsync(e => e.BookingId == bookingId);
    }

    public async Task<IEnumerable<EscrowTransaction>> GetHoldingEscrowsAsync()
    {
        return await entities
            .Include(e => e.Contract)
            .Include(e => e.Payment)
            .Where(e => e.Status == EscrowStatus.Holding)
            .ToListAsync();
    }

    public async Task<IEnumerable<EscrowTransaction>> GetPendingReleaseAsync()
    {
        return await entities
            .Include(e => e.Contract)
            .Include(e => e.Payment)
            .Where(e => e.Status == EscrowStatus.Holding
                        && e.ContractId != null
                        && e.Contract != null
                        && e.Contract.IsAdminApproved)
            .ToListAsync();
    }
}
