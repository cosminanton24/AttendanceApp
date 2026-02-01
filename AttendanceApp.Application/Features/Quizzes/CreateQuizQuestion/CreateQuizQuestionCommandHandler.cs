using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Repositories;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Application.Features.Quizzes.CreateQuizQuestion;

public class CreateQuizQuestionCommandHandler(
    IQuizRepository quizRepo,
    IUserRepository userRepo) 
    : IRequestHandler<CreateQuizQuestionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateQuizQuestionCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepo.GetByIdAsync(command.ProfessorId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with id {command.ProfessorId} found.");

        if (user.Type == UserType.Student)
        {
            throw new ValidationException("User is not authorized to modify quizzes.");
        }

        var quiz = await quizRepo.GetByIdWithQuestionsAsync(command.QuizId, cancellationToken)
            ?? throw new KeyNotFoundException($"No quiz with id {command.QuizId} found.");

        if (quiz.ProfessorId != command.ProfessorId)
        {
            throw new ValidationException("You can only modify your own quizzes.");
        }

        var question = quiz.AddQuestion(command.Text, command.Order, command.Points);

        quizRepo.Update(quiz);
        await quizRepo.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Created(question.Id);
    }
}
