using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.SubmitQuiz;

public record SubmitQuizCommand(
    Guid UserId,
    Guid QuizLectureId) : IRequest<Result<QuizResultDto>>;

public record QuizResultDto(
    decimal Score,
    decimal MaxScore,
    int CorrectQuestions,
    int TotalQuestions);
