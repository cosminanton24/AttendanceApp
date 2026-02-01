using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.CreateQuizQuestion;

public record CreateQuizQuestionCommand(
    Guid ProfessorId,
    Guid QuizId,
    string Text,
    int Order,
    decimal? Points
) : IRequest<Result<Guid>>;
