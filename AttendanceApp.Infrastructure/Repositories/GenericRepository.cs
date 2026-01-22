using AttendanceApp.Domain.Common;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Infrastructure.Repositories;

public class GenericRepository<T> : IRepository<T> where T : Entity<Guid>
{
    protected readonly AttendanceAppAppDbContext _db;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(AttendanceAppAppDbContext db)
    {
        _db = db;
        _dbSet = db.Set<T>();
    }

    public virtual async Task AddAsync(T entry, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entry, ct);
    }

    public virtual void Delete(T entry)
    {
        _dbSet.Remove(entry);
    }

    public virtual async Task DeleteByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _dbSet.FindAsync([id], ct) ?? throw new KeyNotFoundException($"Element {id} cannot be deleted, it does't exist.");
        _dbSet.Remove(entity);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(ct);
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken: ct);
    }

    public virtual async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }

    public virtual void Update(T entry) 
    {
        _dbSet.Update(entry);
    }
}