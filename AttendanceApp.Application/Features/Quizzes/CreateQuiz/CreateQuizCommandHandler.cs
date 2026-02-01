using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Quizzes;
using AttendanceApp.Domain.Repositories;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Application.Features.Quizzes.CreateQuiz;

public class CreateQuizCommandHandler(IQuizRepository quizRepo, IUserRepository userRepo) 
    : IRequestHandler<CreateQuizCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateQuizCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepo.GetByIdAsync(command.ProfessorId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with id {command.ProfessorId} found.");

        if (user.Type == UserType.Student)
        {
            throw new ValidationException("User is not authorized to create a quiz.");
        }

        var quiz = Quiz.Create(
            name: command.Name,
            duration: command.Duration,
            createdByTeacherId: command.ProfessorId,
            createdAtUtc: DateTime.UtcNow
        );

        await quizRepo.AddAsync(quiz, cancellationToken);
        await quizRepo.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Created(quiz.Id);
    }
}
