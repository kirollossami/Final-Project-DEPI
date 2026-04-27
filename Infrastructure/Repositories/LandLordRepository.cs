using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public interface ILandLordRepository : IBaseRepository<LandLord>
{
}

public class LandLordRepository : BaseRepository<LandLord>, ILandLordRepository
{
    public LandLordRepository(StudentHousingDBContext context) : base(context)
    {
    }
}