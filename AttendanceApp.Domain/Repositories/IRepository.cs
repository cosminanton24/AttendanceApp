using AttendanceApp.Domain.Common;

namespace AttendanceApp.Domain.Repositories;

public interface IRepository<T> where T : Entity<Guid>
{
    Task AddAsync(T entry, CancellationToken cancellationToken = default);
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    void Update(T entry);
    void Delete(T entry);
}