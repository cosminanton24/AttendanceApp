using AttendanceApp.Domain.Common;

namespace AttendanceApp.Domain.Repositories;

public interface IRepository<T> where T : Entity<Guid>
{
    Task AddAsync(T entry, CancellationToken ct = default);
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task DeleteByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    void Update(T entry);
    void Delete(T entry);
}