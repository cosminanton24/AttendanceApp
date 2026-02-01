using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Quizzes.Dtos;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetQuizById;

public sealed record GetQuizByIdQuery(Guid QuizId) : IRequest<Result<QuizDetailDto>>;
