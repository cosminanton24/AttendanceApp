using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Lectures.Dtos;
using AttendanceApp.Domain.Enums;
using MediatR;

namespace AttendanceApp.Application.Features.Lectures.UpdateStatus;

public record UpdateLectureStatusCommand(Guid UserId, Guid LectureId, LectureStatus Status, string? Position) : IRequest<Result<LectureDto>>;