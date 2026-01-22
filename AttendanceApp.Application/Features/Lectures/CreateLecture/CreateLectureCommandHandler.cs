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
            throw new ValidationException($"User with id {command.ProfessorId} is not authorized to create a lecture.");
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