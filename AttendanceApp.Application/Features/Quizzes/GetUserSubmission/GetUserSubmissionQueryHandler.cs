using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Repositories;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetUserSubmission;

public class GetUserSubmissionQueryHandler(IUserSubmissionRepository userSubmissionRepo)
    : IRequestHandler<GetUserSubmissionQuery, Result<UserSubmissionDto?>>
{
    public async Task<Result<UserSubmissionDto?>> Handle(
        GetUserSubmissionQuery query,
        CancellationToken cancellationToken)
    {
        var submission = await userSubmissionRepo.GetByUserAndQuizLectureAsync(
            query.UserId,
            query.QuizLectureId,
            cancellationToken);

        if (submission == null)
        {
            return Result<UserSubmissionDto?>.OkNullable(null);
        }

        var dto = new UserSubmissionDto(
            submission.Id,
            submission.UserId,
            submission.QuizLectureId,
            submission.Submitted,
            submission.SubmittedAtUtc,
            submission.Score,
            submission.MaxScore);

        return Result<UserSubmissionDto?>.Ok(dto);
    }
}
