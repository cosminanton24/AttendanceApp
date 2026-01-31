using AttendanceApp.Domain.Users;

namespace AttendanceApp.Domain.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<int> GetTotalUsersByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> SearchUsersByNameAsync(string name, int page, int pageSize, CancellationToken cancellationToken = default);
}