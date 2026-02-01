using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Repositories;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Application.Features.Quizzes.DeleteQuiz;

public class DeleteQuizCommandHandler(IQuizRepository quizRepo, IUserRepository userRepo) 
    : IRequestHandler<DeleteQuizCommand, Result>
{
    public async Task<Result> Handle(DeleteQuizCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepo.GetByIdAsync(command.ProfessorId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with id {command.ProfessorId} found.");

        if (user.Type == UserType.Student)
        {
            throw new ValidationException("User is not authorized to delete quizzes.");
        }

        var quiz = await quizRepo.GetByIdAsync(command.QuizId, cancellationToken)
            ?? throw new KeyNotFoundException($"No quiz with id {command.QuizId} found.");

        if (quiz.ProfessorId != command.ProfessorId)
        {
            throw new ValidationException("You can only delete your own quizzes.");
        }

        quizRepo.Delete(quiz);
        await quizRepo.SaveChangesAsync(cancellationToken);

        return Result.NoContent();
    }
}
