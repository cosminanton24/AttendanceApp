using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.CreateQuizOption;

public record CreateQuizOptionCommand(
    Guid ProfessorId,
    Guid QuestionId,
    string Text,
    int Order,
    bool IsCorrect
) : IRequest<Result<Guid>>;
