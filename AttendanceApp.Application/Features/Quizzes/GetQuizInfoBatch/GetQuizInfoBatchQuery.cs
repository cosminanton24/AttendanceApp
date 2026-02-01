using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Quizzes.Dtos;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetQuizInfoBatch;

public sealed record GetQuizInfoBatchQuery(IReadOnlyList<Guid> Ids)
    : IRequest<Result<IReadOnlyList<QuizDetailDto>>>;
