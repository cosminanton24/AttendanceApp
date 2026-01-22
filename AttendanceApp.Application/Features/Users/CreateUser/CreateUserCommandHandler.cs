
using AttendanceApp.Application.Common.Hash;
using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Domain.Users;
using MediatR;

namespace AttendanceApp.Application.Features.Users.CreateUser;

public class CreateUserCommandHandler(IUserRepository _userRepo) : IRequestHandler<CreateUserCommand, Result>
{
    public async Task<Result> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var newUserId = Guid.NewGuid();
        var hashedPassword = await PasswordHasher.HashPasswordAsync(command.Password);

        var newUser = new User(
            newUserId,
            command.Type,
            command.Name,
            command.Email,
            hashedPassword
        );
        
        await _userRepo.AddAsync(newUser, cancellationToken);
        return Result.Created();
    }
}