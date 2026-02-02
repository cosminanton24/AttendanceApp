using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Quizzes.Dtos;
using AttendanceApp.Domain.Repositories;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetStudentQuizResults;

public class GetStudentQuizResultsQueryHandler(
    IQuizLectureRepository quizLectureRepo,
    IQuizRepository quizRepo,
    IUserSubmissionRepository userSubmissionRepo)
    : IRequestHandler<GetStudentQuizResultsQuery, Result<IReadOnlyList<StudentQuizResultDto>>>
{
    public async Task<Result<IReadOnlyList<StudentQuizResultDto>>> Handle(
        GetStudentQuizResultsQuery query,
        CancellationToken cancellationToken)
    {
        // Get all quiz lectures for this lecture
        var quizLectures = await quizLectureRepo.GetQuizLecturesByLectureIdAsync(
            query.LectureId,
            cancellationToken);

        if (quizLectures.Count == 0)
        {
            return Result<IReadOnlyList<StudentQuizResultDto>>.Ok(
                Array.Empty<StudentQuizResultDto>());
        }

        // Get user submissions for these quiz lectures
        var quizLectureIds = quizLectures.Select(ql => ql.Id).ToList();
        var submissions = await userSubmissionRepo.GetUserSubmissionsForQuizLecturesAsync(
            query.UserId,
            quizLectureIds,
            cancellationToken);

        var submissionDict = submissions.ToDictionary(s => s.QuizLectureId);

        // Get quiz details for each quiz lecture
        var quizIds = quizLectures.Select(ql => ql.QuizId).Distinct().ToList();
        var quizDict = new Dictionary<Guid, string>();

        foreach (var quizId in quizIds)
        {
            var quiz = await quizRepo.GetByIdAsync(quizId, cancellationToken);
            if (quiz != null)
            {
                quizDict[quizId] = quiz.Name;
            }
        }

        // Build the result list
        var results = quizLectures
            .OrderByDescending(ql => ql.CreatedAtUtc)
            .Select(ql =>
            {
                var hasSubmission = submissionDict.TryGetValue(ql.Id, out var submission);
                var hasSubmitted = hasSubmission && submission!.Submitted;

                return new StudentQuizResultDto(
                    QuizLectureId: ql.Id,
                    QuizId: ql.QuizId,
                    QuizName: quizDict.GetValueOrDefault(ql.QuizId, "Quiz"),
                    ActivatedAtUtc: ql.CreatedAtUtc,
                    HasSubmitted: hasSubmitted,
                    Score: hasSubmitted ? submission!.Score : null,
                    MaxScore: hasSubmitted ? submission!.MaxScore : null
                );
            })
            .ToList();

        return Result<IReadOnlyList<StudentQuizResultDto>>.Ok(results);
    }
}
