using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Repositories;
using MediatR;
using AttendanceApp.Application.Features.Lectures.Dtos;
using AttendanceApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Application.Features.Lectures.GetStudentLectures;

public class GetStudentLecturesCommandQuery(ILectureRepository _lectureRepo, IUserRepository _userRepo) : IRequestHandler<GetStudentLecturesQuery, Result<IReadOnlyList<LectureDto>>>
{
    public async Task<Result<IReadOnlyList<LectureDto>>> Handle(GetStudentLecturesQuery command, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByIdAsync(command.StudentId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with id {command.StudentId} found.");

        if(user.Type == UserType.Professor)
        {
            throw new ValidationException($"User with id {command.StudentId} is not authorized to have student lectures.");
        }


        var lectures = await _lectureRepo.GetStudentLecturesAsync(
            command.StudentId, 
            command.PageNumber, 
            command.PageSize, 
            command.FromMonthsAgo.HasValue ? DateTime.UtcNow.AddMonths(-command.FromMonthsAgo.Value) : null,
            command.Status,
            cancellationToken);

        var lectureDtos = lectures.Select(lecture => new LectureDto(
            lecture.Id,
            lecture.ProfessorId,
            lecture.Name,
            lecture.Description,
            lecture.Status,
            lecture.StartTime,
            lecture.Duration,
            lecture.Attendees.Count
        )).ToList();

        return Result<IReadOnlyList<LectureDto>>.Ok(lectureDtos);
    }
}