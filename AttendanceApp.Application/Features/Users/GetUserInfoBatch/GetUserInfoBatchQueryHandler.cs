using System.ComponentModel.DataAnnotations;
using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Users.Dtos;
using AttendanceApp.Domain.Repositories;
using MediatR;

namespace AttendanceApp.Application.Features.Users.GetUserInfoBatch;

public sealed class GetUserInfoBatchQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserInfoBatchQuery, Result<IReadOnlyList<UserInfoDto>>>
{
    public async Task<Result<IReadOnlyList<UserInfoDto>>> Handle(GetUserInfoBatchQuery request, CancellationToken cancellationToken)
    {
        if (request.Ids is null || request.Ids.Count == 0)
            throw new ValidationException("At least one user id is required.");

        var distinctIds = request.Ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (distinctIds.Length == 0)
            throw new ValidationException("At least one user id is required.");

        var users = await userRepository.GetByIdsAsync(distinctIds, cancellationToken);

        var byId = users.ToDictionary(u => u.Id, u => new UserInfoDto(u.Id, u.Name, u.Email));

        var dtos = new List<UserInfoDto>(request.Ids.Count);
        var missing = new HashSet<Guid>();

        foreach (var id in request.Ids)
        {
            if (id == Guid.Empty) continue;
            if (byId.TryGetValue(id, out var dto))
            {
                dtos.Add(dto);
            }
            else
            {
                missing.Add(id);
            }
        }

        if (missing.Count > 0)
            throw new KeyNotFoundException($"No account(s) found for id(s): {string.Join(", ", missing)}");

        return Result<IReadOnlyList<UserInfoDto>>.Ok(dtos);
    }
}
