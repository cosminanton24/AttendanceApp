using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Repositories;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Application.Features.Quizzes.DeleteQuizOption;

public class DeleteQuizOptionCommandHandler(
    IQuizOptionRepository optionRepo,
    IQuizQuestionRepository questionRepo,
    IQuizRepository quizRepo,
    IUserRepository userRepo) 
    : IRequestHandler<DeleteQuizOptionCommand, Result>
{
    public async Task<Result> Handle(DeleteQuizOptionCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepo.GetByIdAsync(command.ProfessorId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with id {command.ProfessorId} found.");

        if (user.Type == UserType.Student)
        {
            throw new ValidationException("User is not authorized to modify quizzes.");
        }

        var option = await optionRepo.GetByIdAsync(command.OptionId, cancellationToken)
            ?? throw new KeyNotFoundException($"No option with id {command.OptionId} found.");

        var question = await questionRepo.GetByIdAsync(option.QuestionId, cancellationToken)
            ?? throw new KeyNotFoundException($"No question found for option {command.OptionId}.");

        var quiz = await quizRepo.GetByIdAsync(question.QuizId, cancellationToken)
            ?? throw new KeyNotFoundException($"No quiz found for question {option.QuestionId}.");

        if (quiz.ProfessorId != command.ProfessorId)
        {
            throw new ValidationException("You can only modify your own quizzes.");
        }

        optionRepo.Delete(option);
        await optionRepo.SaveChangesAsync(cancellationToken);

        return Result.NoContent();
    }
}
