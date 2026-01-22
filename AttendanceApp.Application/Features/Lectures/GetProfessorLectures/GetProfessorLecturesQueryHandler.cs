
using AttendanceApp.Application.Common.Hash;
using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Domain.Lectures;
using MediatR;
using AttendanceApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using AttendanceApp.Application.Features.Lectures.Dtos;

namespace AttendanceApp.Application.Features.Lectures.GetProfessorLectures;

public class GetProfessorLecturesCommandQuery(ILectureRepository _lectureRepo, IUserRepository _userRepo) : IRequestHandler<GetProfessorLecturesQuery, Result<IReadOnlyList<LectureDto>>>
{
    public async Task<Result<IReadOnlyList<LectureDto>>> Handle(GetProfessorLecturesQuery command, CancellationToken cancellationToken)
    {
        _ = await _userRepo.GetByIdAsync(command.ProfessorId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with id {command.ProfessorId} found.");


        var lectures = await _lectureRepo.GetProfessorLecturesAsync(
            command.ProfessorId, 
            command.PageNumber, 
            command.PageSize, 
            command.FromMonthsAgo.HasValue ? DateTime.UtcNow.AddMonths(-command.FromMonthsAgo.Value) : null,
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