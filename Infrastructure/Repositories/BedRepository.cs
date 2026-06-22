using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public interface IBedRepository : IBaseRepository<Bed>
{
}

public class BedRepository : BaseRepository<Bed>, IBedRepository
{
    public BedRepository(StudentHousingDBContext context) : base(context)
    {
    }
}
