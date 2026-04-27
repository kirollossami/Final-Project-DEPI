using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public interface IRoomRepository : IBaseRepository<Room>
{
}

public class RoomRepository : BaseRepository<Room>, IRoomRepository
{
    public RoomRepository(StudentHousingDBContext context) : base(context)
    {
    }
}
