using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.CreateQuiz;

public record CreateQuizCommand(
    Guid ProfessorId,
    string Name,
    TimeSpan Duration
) : IRequest<Result<Guid>>;
