using System.ComponentModel.DataAnnotations;
using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.LectureAttendees.Dtos;
using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Repositories;
using MediatR;

namespace AttendanceApp.Application.Features.LectureAttendees.GetLectureAttendees;

public sealed class GetLectureAttendeesQueryHandler(
    IUserRepository userRepository,
    ILectureRepository lectureRepository,
    ILectureAttendeeRepository lectureAttendeeRepository)
    : IRequestHandler<GetLectureAttendeesQuery, Result<LectureAttendeesPageDto>>
{
    public async Task<Result<LectureAttendeesPageDto>> Handle(GetLectureAttendeesQuery request, CancellationToken cancellationToken)
    {
        if (request.RequesterId == Guid.Empty)
            throw new ValidationException("Requester id is required.");

        if (request.LectureId == Guid.Empty)
            throw new ValidationException("Lecture id is required.");

        var requester = await userRepository.GetByIdAsync(request.RequesterId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with id {request.RequesterId} found.");

        if (requester.Type == UserType.Student)
            throw new ValidationException("User is not authorized to view lecture attendees.");

        var lecture = await lectureRepository.GetByIdAsync(request.LectureId, cancellationToken)
            ?? throw new KeyNotFoundException($"No lecture with id {request.LectureId} found.");

        if (requester.Type == UserType.Professor && lecture.ProfessorId != requester.Id)
            throw new ValidationException("User is not authorized to view attendees for this lecture.");

        var total = await lectureAttendeeRepository.GetTotalAttendeesAsync(request.LectureId, request.SearchFilter, cancellationToken);
        var attendees = await lectureAttendeeRepository.GetLectureAttendeesAsync(request.LectureId, request.Page, request.PageSize, request.SearchFilter, cancellationToken);

        var items = attendees
            .Select(a => new LectureAttendeeDto(a.UserId, a.TimeJoined))
            .ToList();

        return Result<LectureAttendeesPageDto>.Ok(new LectureAttendeesPageDto(items, total));
    }
}
