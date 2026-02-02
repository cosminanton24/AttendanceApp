using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetSubmissionCount;

public record GetSubmissionCountQuery(Guid QuizLectureId) : IRequest<Result<int>>;
