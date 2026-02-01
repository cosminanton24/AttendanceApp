using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.DeleteQuizOption;

public record DeleteQuizOptionCommand(Guid ProfessorId, Guid OptionId) : IRequest<Result>;
