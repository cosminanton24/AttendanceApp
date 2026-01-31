using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Users.Dtos;
using AttendanceApp.Domain.Repositories;
using MediatR;

namespace AttendanceApp.Application.Features.Users.SearchUsersByName;

public sealed class SearchUsersByNameQueryHandler(IUserRepository userRepository)
    : IRequestHandler<SearchUsersByNameQuery, Result<UsersPageDto>>
{
    public async Task<Result<UsersPageDto>> Handle(SearchUsersByNameQuery request, CancellationToken cancellationToken)
    {
        var total = await userRepository.GetTotalUsersByNameAsync(request.Name, cancellationToken);
        var users = await userRepository.SearchUsersByNameAsync(request.Name, request.Page, request.PageSize, cancellationToken);

        var items = users
            .Select(u => new UserInfoDto(u.Id, u.Name, u.Email))
            .ToList();

        return Result<UsersPageDto>.Ok(new UsersPageDto(items, total));
    }
}
