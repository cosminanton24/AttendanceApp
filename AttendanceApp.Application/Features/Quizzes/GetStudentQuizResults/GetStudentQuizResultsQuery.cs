using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Quizzes.Dtos;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetStudentQuizResults;

public sealed record GetStudentQuizResultsQuery(
    Guid UserId,
    Guid LectureId
) : IRequest<Result<IReadOnlyList<StudentQuizResultDto>>>;
