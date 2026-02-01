using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Quizzes.Dtos;
using AttendanceApp.Domain.Repositories;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetQuizzesByLecture;

public class GetQuizzesByLectureQueryHandler(IQuizLectureRepository quizLectureRepo, ILectureRepository lectureRepo)
    : IRequestHandler<GetQuizzesByLectureQuery, Result<IReadOnlyList<QuizLectureDto>>>
{
    public async Task<Result<IReadOnlyList<QuizLectureDto>>> Handle(GetQuizzesByLectureQuery query, CancellationToken cancellationToken)
    {
        var lecture = await lectureRepo.GetByIdAsync(query.LectureId, cancellationToken)
            ?? throw new KeyNotFoundException($"No lecture with id {query.LectureId} found.");

        var quizLectures = await quizLectureRepo.GetQuizLecturesByLectureIdAsync(query.LectureId, cancellationToken);

        var now = DateTime.UtcNow;
        var dtos = quizLectures.Select(ql => new QuizLectureDto(
            ql.Id,
            ql.LectureId,
            ql.QuizId,
            ql.CreatedAtUtc,
            ql.EndTimeUtc,
            ql.IsActive(now)
        )).ToList();

        return Result<IReadOnlyList<QuizLectureDto>>.Ok(dtos);
    }
}
