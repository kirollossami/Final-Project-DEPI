using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public interface IReviewRepository : IBaseRepository<Review>
{
}

public class ReviewRepository : BaseRepository<Review>, IReviewRepository
{
    public ReviewRepository(StudentHousingDBContext context) : base(context)
    {
    }
}
