using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Base;

public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    internal readonly StudentHousingDBContext context;
    private DbSet<T> entities;
    public BaseRepository(StudentHousingDBContext context)
    {
        this.context = context;
        entities = context.Set<T>();
    }
    public virtual IQueryable<T> GetAll(bool asNoTracking = false)
    {
        if (asNoTracking)
            return entities.AsQueryable().AsNoTracking();

        return entities.AsQueryable();
    }
    public async Task Insert(T entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException("entity");
        }

        await entities.AddAsync(entity);
    }
    public async Task CommitAsync()
    {
        await context.SaveChangesAsync();

    }
    public async Task Update(T entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException("entity");
        }

        await Task.Run(() => context.Update(entity));
    }
    public async Task Delete(T entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException("entity");
        }
        await Task.Run(() => entities.Remove(entity));
    }
    public async Task<T?> GetAsync(object id)
    {
        return await entities.FindAsync(id);
    }
}