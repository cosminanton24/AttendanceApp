using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.UpdateQuizQuestion;

public record UpdateQuizQuestionCommand(
    Guid ProfessorId,
    Guid QuestionId,
    string? Text,
    decimal? Points
) : IRequest<Result>;
