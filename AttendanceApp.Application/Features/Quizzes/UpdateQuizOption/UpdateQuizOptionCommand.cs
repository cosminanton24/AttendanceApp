using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.UpdateQuizOption;

public record UpdateQuizOptionCommand(
    Guid ProfessorId,
    Guid OptionId,
    bool? IsCorrect,
    string? Text
) : IRequest<Result>;
