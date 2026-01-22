using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Users.JoinLecture;

public record JoinLectureCommand(Guid UserId, Guid LectureId) : IRequest<Result>;