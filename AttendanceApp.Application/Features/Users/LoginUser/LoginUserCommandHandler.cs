
using System.ComponentModel.DataAnnotations;
using AttendanceApp.Application.Common.Hash;
using AttendanceApp.Application.Common.Results;
using AttendanceApp.Core.Application.Common.Jwt;
using AttendanceApp.Domain.Repositories;
using MediatR;

namespace AttendanceApp.Application.Features.Users.LoginUser;

public class LoginUserCommandHandler(IUserRepository _userRepo) : IRequestHandler<LoginUserCommand, Result<string>>
{
    public async Task<Result<string>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByEmailAsync(command.Email, cancellationToken) 
            ?? throw new KeyNotFoundException($"No account with email {command.Email} found.");

        if(!await PasswordHasher.VerifyPasswordAsync(user.Password, command.Password))
        {
            throw new ValidationException($"Incorrect password for {command.Email}.");
        }
        
        var jwt = JwtTokenGenerator.GenerateToken(user.Id.ToString(), command.Email);
        return Result<string>.Ok(jwt);
    }
}