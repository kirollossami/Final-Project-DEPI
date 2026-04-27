using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public interface IBookingRepository : IBaseRepository<Booking>
{
}

public class BookingRepository : BaseRepository<Booking>, IBookingRepository
{
    public BookingRepository(StudentHousingDBContext context) : base(context)
    {
    }
}
