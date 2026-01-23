using AttendanceApp.Domain.Common;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Infrastructure.Repositories;

public class GenericRepository<T> : IRepository<T> where T : Entity<Guid>
{
    protected readonly AttendanceAppDbContext _db;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(AttendanceAppDbContext db)
    {
        _db = db;
        _dbSet = db.Set<T>();
    }

    public virtual async Task AddAsync(T entry, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entry, cancellationToken);
    }

    public virtual void Delete(T entry)
    {
        _dbSet.Remove(entry);
    }

    public virtual async Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FindAsync([id], cancellationToken) ?? throw new KeyNotFoundException($"Element {id} cannot be deleted, it does't exist.");
        _dbSet.Remove(entity);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken: cancellationToken);
    }

    public virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }

    public virtual void Update(T entry) 
    {
        _dbSet.Update(entry);
    }
}