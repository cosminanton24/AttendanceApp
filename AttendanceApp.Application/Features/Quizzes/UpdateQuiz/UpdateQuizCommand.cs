using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.UpdateQuiz;

public record UpdateQuizCommand(
    Guid ProfessorId,
    Guid QuizId,
    string? Name,
    TimeSpan? Duration
) : IRequest<Result>;
