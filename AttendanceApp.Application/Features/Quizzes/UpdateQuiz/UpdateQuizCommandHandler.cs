using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Repositories;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Application.Features.Quizzes.UpdateQuiz;

public class UpdateQuizCommandHandler(IQuizRepository quizRepo, IUserRepository userRepo) 
    : IRequestHandler<UpdateQuizCommand, Result>
{
    public async Task<Result> Handle(UpdateQuizCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepo.GetByIdAsync(command.ProfessorId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with id {command.ProfessorId} found.");

        if (user.Type == UserType.Student)
        {
            throw new ValidationException("User is not authorized to modify quizzes.");
        }

        var quiz = await quizRepo.GetByIdAsync(command.QuizId, cancellationToken)
            ?? throw new KeyNotFoundException($"No quiz with id {command.QuizId} found.");

        if (quiz.ProfessorId != command.ProfessorId)
        {
            throw new ValidationException("You can only modify your own quizzes.");
        }

        if (!string.IsNullOrWhiteSpace(command.Name))
        {
            quiz.Rename(command.Name);
        }

        if (command.Duration.HasValue)
        {
            quiz.ChangeDuration(command.Duration.Value);
        }

        quizRepo.Update(quiz);
        await quizRepo.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
