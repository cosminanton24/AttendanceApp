using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Quizzes.Dtos;
using AttendanceApp.Domain.Repositories;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetQuizSubmissions;

public class GetQuizSubmissionsQueryHandler(
    IUserSubmissionRepository submissionRepo,
    IUserRepository userRepo
) : IRequestHandler<GetQuizSubmissionsQuery, Result<QuizSubmissionsPageDto>>
{
    public async Task<Result<QuizSubmissionsPageDto>> Handle(
        GetQuizSubmissionsQuery query,
        CancellationToken cancellationToken)
    {
        var submissions = await submissionRepo.GetSubmissionsByQuizLecturePagedAsync(
            query.QuizLectureId,
            query.PageNumber,
            query.PageSize,
            query.SearchTerm,
            cancellationToken);

        var totalCount = await submissionRepo.GetSubmissionsTotalCountAsync(
            query.QuizLectureId,
            query.SearchTerm,
            cancellationToken);

        // Get user info for each submission
        var userIds = submissions.Select(s => s.UserId).Distinct().ToList();
        var users = await userRepo.GetByIdsAsync(userIds, cancellationToken);
        var userMap = users.ToDictionary(u => u.Id);

        var dtos = submissions.Select(s =>
        {
            var user = userMap.GetValueOrDefault(s.UserId);
            return new QuizSubmissionDto(
                s.Id,
                s.UserId,
                user?.Name ?? "Unknown",
                user?.Email ?? "",
                s.Submitted,
                s.SubmittedAtUtc,
                s.Score,
                s.MaxScore
            );
        }).ToList();

        return Result<QuizSubmissionsPageDto>.Ok(new QuizSubmissionsPageDto(
            dtos,
            totalCount,
            query.PageNumber,
            query.PageSize
        ));
    }
}
