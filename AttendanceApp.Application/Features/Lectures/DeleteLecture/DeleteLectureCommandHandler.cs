using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Repositories;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Application.Features.Lectures.DeleteLecture;

public class DeleteLectureCommandHandler(ILectureRepository _lectureRepo, IUserRepository _userRepo) : IRequestHandler<DeleteLectureCommand, Result>
{
    public async Task<Result> Handle(DeleteLectureCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with id {command.UserId} found.");

        if (user.Type == UserType.Student)
        {
            throw new ValidationException($"User with id {command.UserId} is not authorized to delete lectures.");
        }

        var lecture = await _lectureRepo.GetByIdAsync(command.LectureId, cancellationToken)
            ?? throw new KeyNotFoundException($"No lecture with id {command.LectureId} found.");

        if (lecture.ProfessorId != command.UserId)
        {
            throw new ValidationException("You can only delete your own lectures.");
        }

        if (lecture.Status != LectureStatus.Canceled)
        {
            throw new ValidationException("Only canceled lectures can be deleted.");
        }

        _lectureRepo.Delete(lecture);
        await _lectureRepo.SaveChangesAsync(cancellationToken);

        return Result.NoContent();
    }
}
