using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public interface IHousingUnitRepository : IBaseRepository<HousingUnit>
{
}

public class HousingUnitRepository : BaseRepository<HousingUnit>, IHousingUnitRepository
{
    public HousingUnitRepository(StudentHousingDBContext context) : base(context)
    {
    }
}
