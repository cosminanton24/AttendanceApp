using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Domain.Lectures;
using MediatR;
using AttendanceApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Application.Features.Lectures.CreateLecture;

public class CreateLectureCommandHandler(ILectureRepository _lectureRepo, IUserRepository _userRepo) : IRequestHandler<CreateLectureCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateLectureCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByIdAsync(command.ProfessorId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with id {command.ProfessorId} found.");

        if(user.Type == UserType.Student)
        {
            throw new ValidationException($"User is not authorized to create a lecture.");
        }
        
        if(await _lectureRepo.HasProfessorConflictingLectureAsync(user.Id, command.StartTime, command.StartTime.Add(command.Duration), cancellationToken))
        {
            throw new ValidationException("You have a conflicting lecture scheduled during this time.");
        }

        var newLectureId = Guid.NewGuid();
        var newLecture = new Lecture(
            newLectureId,
            user.Id,
            command.Name,
            command.Description,
            command.StartTime,
            command.Duration
        );

        await _lectureRepo.AddAsync(newLecture, cancellationToken);
        await _lectureRepo.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Created(newLectureId);
    }
}