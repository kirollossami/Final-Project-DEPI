using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Base;


public interface IBaseRepository<T> where T : class
{

    IQueryable<T> GetAll(bool asNoTracking = false);
    Task<T?> GetAsync(object id);
    Task Insert(T entity);
    Task Update(T entity);
    Task Delete(T entity);
    Task CommitAsync();
}