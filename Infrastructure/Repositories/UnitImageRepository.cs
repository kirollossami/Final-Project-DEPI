using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;

namespace Infrastructure.Repositories;

public interface IUnitImageRepository : IBaseRepository<UnitImage>
{
}

public class UnitImageRepository : BaseRepository<UnitImage>, IUnitImageRepository
{
    public UnitImageRepository(StudentHousingDBContext context) : base(context)
    {
    }
}
