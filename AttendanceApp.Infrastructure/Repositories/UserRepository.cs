using AttendanceApp.Domain.Repositories;
using AttendanceApp.Domain.Users;
using AttendanceApp.Infrastructure.Persistence;

namespace AttendanceApp.Infrastructure.Repositories;

public class UserRepository(AttendanceAppAppDbContext db) : GenericRepository<User>(db), IUserRepository
{
    
}