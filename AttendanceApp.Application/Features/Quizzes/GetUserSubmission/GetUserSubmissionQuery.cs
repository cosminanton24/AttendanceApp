using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetUserSubmission;

public record GetUserSubmissionQuery(
    Guid UserId,
    Guid QuizLectureId) : IRequest<Result<UserSubmissionDto?>>;

public record UserSubmissionDto(
    Guid Id,
    Guid UserId,
    Guid QuizLectureId,
    bool Submitted,
    DateTime SubmittedAtUtc,
    decimal Score,
    decimal MaxScore);
