using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Quizzes.Dtos;
using AttendanceApp.Domain.Repositories;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetActiveQuizForLecture;

public class GetActiveQuizForLectureQueryHandler(
    IQuizLectureRepository quizLectureRepo,
    IQuizRepository quizRepo,
    ILectureRepository lectureRepo)
    : IRequestHandler<GetActiveQuizForLectureQuery, Result<ActiveQuizDto?>>
{
    public async Task<Result<ActiveQuizDto?>> Handle(GetActiveQuizForLectureQuery query, CancellationToken cancellationToken)
    {
        _ = await lectureRepo.GetByIdAsync(query.LectureId, cancellationToken)
            ?? throw new KeyNotFoundException($"No lecture with id {query.LectureId} found.");

        var now = DateTime.UtcNow;
        var activeQuizLecture = await quizLectureRepo.GetActiveQuizForLectureAsync(query.LectureId, now, cancellationToken);

        if (activeQuizLecture is null)
        {
            return Result<ActiveQuizDto?>.OkNullable(null);
        }

        var quiz = await quizRepo.GetByIdWithQuestionsAndOptionsAsync(activeQuizLecture.QuizId, cancellationToken);

        if (quiz is null)
        {
            return Result<ActiveQuizDto?>.OkNullable(null);
        }

        var questionDtos = quiz.Questions.Select(q => new QuizQuestionDto(
            q.Id,
            q.QuizId,
            q.Text,
            q.Order,
            q.Points,
            q.Options.Select(o => new QuizOptionDto(
                o.Id,
                o.Text,
                o.Order,
                o.IsCorrect
            )).ToList()
        )).ToList();

        var dto = new ActiveQuizDto(
            quiz.Id,
            activeQuizLecture.Id,
            quiz.Name,
            quiz.Duration,
            quiz.ProfessorId,
            quiz.CreatedAtUtc,
            activeQuizLecture.CreatedAtUtc,
            activeQuizLecture.EndTimeUtc,
            questionDtos
        );

        return Result<ActiveQuizDto?>.Ok(dto);
    }
}
