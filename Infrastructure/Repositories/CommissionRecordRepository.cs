using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;

namespace Infrastructure.Repositories;

public interface ICommissionRecordRepository : IBaseRepository<CommissionRecord> { }

public class CommissionRecordRepository : BaseRepository<CommissionRecord>, ICommissionRecordRepository
{
    public CommissionRecordRepository(StudentHousingDBContext context) : base(context) { }
}
