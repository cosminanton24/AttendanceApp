using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.DeleteQuizQuestion;

public record DeleteQuizQuestionCommand(Guid ProfessorId, Guid QuestionId) : IRequest<Result>;
