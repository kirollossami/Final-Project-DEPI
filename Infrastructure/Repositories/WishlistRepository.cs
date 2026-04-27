using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public interface IWishlistRepository : IBaseRepository<Wishlist>
{
}

public class WishlistRepository : BaseRepository<Wishlist>, IWishlistRepository
{
    public WishlistRepository(StudentHousingDBContext context) : base(context)
    {
    }
}
