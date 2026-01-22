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