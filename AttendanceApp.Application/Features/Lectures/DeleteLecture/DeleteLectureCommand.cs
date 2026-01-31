using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Lectures.DeleteLecture;

public record DeleteLectureCommand(Guid UserId, Guid LectureId) : IRequest<Result>;
