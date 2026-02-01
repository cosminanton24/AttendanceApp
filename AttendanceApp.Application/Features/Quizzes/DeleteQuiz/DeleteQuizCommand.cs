using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.DeleteQuiz;

public record DeleteQuizCommand(Guid ProfessorId, Guid QuizId) : IRequest<Result>;
