using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Repositories;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetSubmissionCount;

public class GetSubmissionCountQueryHandler(IUserSubmissionRepository userSubmissionRepo)
    : IRequestHandler<GetSubmissionCountQuery, Result<int>>
{
    public async Task<Result<int>> Handle(
        GetSubmissionCountQuery query,
        CancellationToken cancellationToken)
    {
        var count = await userSubmissionRepo.GetSubmissionCountAsync(
            query.QuizLectureId,
            cancellationToken);

        return Result<int>.Ok(count);
    }
}
