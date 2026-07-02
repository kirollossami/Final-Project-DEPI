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

public interface IContractRepository : IBaseRepository<Contract>
{
    Task<Contract?> GetByBookingIdAsync(Guid bookingId);
    Task<IEnumerable<Contract>> GetPendingContractsAsync();
    Task<IEnumerable<Contract>> GetAwaitingAdminApprovalAsync();
}

public class ContractRepository : BaseRepository<Contract>, IContractRepository
{
    public ContractRepository(StudentHousingDBContext context) : base(context)
    {
    }

    public async Task<Contract?> GetByBookingIdAsync(Guid bookingId)
    {
        return await entities
            .Include(c => c.Booking)
            .FirstOrDefaultAsync(c => c.BookingId == bookingId);
    }

    public async Task<IEnumerable<Contract>> GetPendingContractsAsync()
    {
        return await entities
            .Include(c => c.Booking)
            .ThenInclude(b => b.Student)
            .Where(c => !c.IsStudentSigned || !c.IsOwnerSigned)
            .ToListAsync();
    }

    public async Task<IEnumerable<Contract>> GetAwaitingAdminApprovalAsync()
    {
        return await entities
            .Include(c => c.Booking)
            .ThenInclude(b => b.Student)
            .Where(c => c.IsStudentSigned && c.IsOwnerSigned && !c.IsAdminApproved)
            .ToListAsync();
    }
}
