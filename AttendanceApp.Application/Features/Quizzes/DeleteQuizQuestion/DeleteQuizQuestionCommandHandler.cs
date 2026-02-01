using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Repositories;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Application.Features.Quizzes.DeleteQuizQuestion;

public class DeleteQuizQuestionCommandHandler(
    IQuizQuestionRepository questionRepo,
    IQuizRepository quizRepo,
    IUserRepository userRepo) 
    : IRequestHandler<DeleteQuizQuestionCommand, Result>
{
    public async Task<Result> Handle(DeleteQuizQuestionCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepo.GetByIdAsync(command.ProfessorId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with id {command.ProfessorId} found.");

        if (user.Type == UserType.Student)
        {
            throw new ValidationException("User is not authorized to modify quizzes.");
        }

        var question = await questionRepo.GetByIdAsync(command.QuestionId, cancellationToken)
            ?? throw new KeyNotFoundException($"No question with id {command.QuestionId} found.");

        var quiz = await quizRepo.GetByIdAsync(question.QuizId, cancellationToken)
            ?? throw new KeyNotFoundException($"No quiz found for question {command.QuestionId}.");

        if (quiz.ProfessorId != command.ProfessorId)
        {
            throw new ValidationException("You can only modify your own quizzes.");
        }

        questionRepo.Delete(question);
        await questionRepo.SaveChangesAsync(cancellationToken);

        return Result.NoContent();
    }
}
