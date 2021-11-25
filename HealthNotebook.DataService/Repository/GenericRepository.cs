using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthNotebook.DataService.Data;
using HealthNotebook.DataService.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthNotebook.DataService.Repository;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected AppDbContext _context;
    internal DbSet<T> dbSet;
    protected readonly ILogger _logger;

    public GenericRepository(
        AppDbContext context,
        ILogger logger
    )
    {
        _context = context;
        _logger = logger;
        dbSet = context.Set<T>();
    }

    public virtual async Task<bool> Add(T entity)
    {
        await dbSet.AddAsync(entity);
        return true;
    }

    public virtual async Task<IEnumerable<T>> All()
    {
        return await dbSet.ToListAsync();
    }

    public virtual Task<bool> Delete(Guid id, string userId)
    {
        throw new NotImplementedException();
    }

    public virtual async Task<T> GetById(Guid id)
    {
        return await dbSet.FindAsync(id);
    }

    public Task<bool> Upsert(T entity)
    {
        throw new NotImplementedException();
    }
}