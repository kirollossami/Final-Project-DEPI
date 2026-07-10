using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;

namespace Infrastructure.Repositories;

public class BalanceRepository : BaseRepository<Balance>, IBalanceRepository
{
    public BalanceRepository(StudentHousingDBContext context) : base(context)
    {
    }
}
