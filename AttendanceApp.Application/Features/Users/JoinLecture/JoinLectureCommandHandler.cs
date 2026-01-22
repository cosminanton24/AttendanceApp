
using System.ComponentModel.DataAnnotations;
using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Lectures;
using AttendanceApp.Domain.Repositories;
using MediatR;

namespace AttendanceApp.Application.Features.Users.JoinLecture;

public class JoinLectureCommandHandler(IUserRepository _userRepo, ILectureRepository _lectureRepo, ILectureAttendeeRepository _lectureAttendeeRepository) : IRequestHandler<JoinLectureCommand, Result>
{
    public async Task<Result> Handle(JoinLectureCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with ID {command.UserId} found.");

        var alreadyJoined = await _lectureAttendeeRepository.GetAttendeeAsync(command.LectureId, command.UserId, cancellationToken);
        if (alreadyJoined != null)
        {
            throw new ValidationException($"User with ID {command.UserId} has already joined lecture with ID {command.LectureId}.");
        }

        var userOngoingLectures = await _lectureRepo.GetStudentLecturesAsync(command.UserId, 0, 1, null, LectureStatus.InProgress, cancellationToken);
        if(userOngoingLectures.Count > 0)
        {
            throw new ValidationException($"User with ID {command.UserId} has already joined a lecture.");
        }


        var lecture = await _lectureRepo.GetByIdAsync(command.LectureId, cancellationToken)
            ?? throw new KeyNotFoundException($"No lecture with ID {command.LectureId} found.");

        if(lecture.Status != LectureStatus.InProgress)
            throw new ValidationException($"Lecture with ID {command.LectureId} is not in progress.");

        if(user.Type != UserType.Student)
            throw new ValidationException($"You can only join lectures as a student");

        var newLecture = new LectureAttendee
        (
            lecture.Id,
            user.Id
        );

        await _lectureAttendeeRepository.AddAsync(newLecture, cancellationToken);
        await _lectureAttendeeRepository.SaveChangesAsync(cancellationToken);

        return Result.Created();
    }
}