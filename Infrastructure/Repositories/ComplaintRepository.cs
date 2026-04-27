using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public interface IComplaintRepository : IBaseRepository<Complaint>
{
}

public class ComplaintRepository : BaseRepository<Complaint>, IComplaintRepository
{
    public ComplaintRepository(StudentHousingDBContext context) : base(context)
    {
    }
}
