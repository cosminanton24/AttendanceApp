using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.LectureAttendees.Dtos;
using MediatR;

namespace AttendanceApp.Application.Features.LectureAttendees.GetLectureAttendees;

public sealed record GetLectureAttendeesQuery(
    Guid RequesterId,
    Guid LectureId,
    int Page,
    int PageSize
) : IRequest<Result<LectureAttendeesPageDto>>;
