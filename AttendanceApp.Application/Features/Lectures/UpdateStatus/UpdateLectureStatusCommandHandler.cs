using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Repositories;
using MediatR;
using AttendanceApp.Application.Features.Lectures.Dtos;
using AttendanceApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Application.Features.Lectures.UpdateStatus;

public class UpdateLectureStatusCommandHandler(ILectureRepository _lectureRepo, IUserRepository _userRepo) : IRequestHandler<UpdateLectureStatusCommand, Result<LectureDto>>
{
    public async Task<Result<LectureDto>> Handle(UpdateLectureStatusCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with id {command.UserId} found.");

        if(user.Type == UserType.Student)
        {
            throw new ValidationException($"User with id {command.UserId} is not authorized to change lectures.");
        }

        var lecture = await _lectureRepo.GetByIdAsync(command.LectureId, cancellationToken)
            ?? throw new KeyNotFoundException($"No lecture with id {command.LectureId} found.");


        if(lecture.Status == LectureStatus.Canceled || lecture.Status == LectureStatus.Ended)
        {
            throw new ValidationException("Cannot change status of a canceled or ended lecture.");
        }
        if(command.Status == LectureStatus.Ended && lecture.Status != LectureStatus.InProgress)
        {
            throw new ValidationException("Only lectures in progress can be ended.");
        }
        if(command.Status == LectureStatus.InProgress && DateTime.UtcNow < lecture.StartTime.AddMinutes(-15))
        {
            throw new ValidationException("Cannot start lecture more than 15 minutes before its scheduled start time.");
        }
        if(command.Status == LectureStatus.InProgress && DateTime.UtcNow > lecture.EndTime.AddMinutes(15))
        {
            throw new ValidationException("Cannot start lecture more than 15 minutes after its scheduled end time.");
        }        
        if(command.Status == LectureStatus.Canceled && lecture.Status != LectureStatus.Scheduled)
        {
            throw new ValidationException("Only scheduled lectures can be canceled.");
        }

        lecture.ChangeStatus(command.Status);
        await _lectureRepo.SaveChangesAsync(cancellationToken);
        var lectureDto = new LectureDto
        (
            lecture.Id,
            lecture.ProfessorId,
            lecture.Name,
            lecture.Description,
            lecture.Status,
            lecture.StartTime,
            lecture.Duration,
            null
        );

        return Result<LectureDto>.Ok(lectureDto);
    }
}