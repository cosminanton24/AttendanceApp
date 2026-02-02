using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Quizzes;
using AttendanceApp.Domain.Repositories;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Application.Features.Quizzes.SaveUserAnswer;

public class SaveUserAnswerCommandHandler(
    IUserAnswerRepository userAnswerRepo,
    IQuizLectureRepository quizLectureRepo,
    IUserRepository userRepo)
    : IRequestHandler<SaveUserAnswerCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SaveUserAnswerCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepo.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with id {command.UserId} found.");

        if (user.Type != UserType.Student)
        {
            throw new ValidationException("Only students can submit quiz answers.");
        }

        var quizLecture = await quizLectureRepo.GetByIdAsync(command.QuizLectureId, cancellationToken)
            ?? throw new KeyNotFoundException($"No quiz lecture with id {command.QuizLectureId} found.");

        var now = DateTime.UtcNow;
        if (!quizLecture.IsActive(now))
        {
            throw new ValidationException("This quiz is no longer active.");
        }

        // Check if user already has an answer for this question/option combination
        var existingAnswer = await userAnswerRepo.GetByUserQuizLectureQuestionOptionAsync(
            command.UserId,
            command.QuizLectureId,
            command.QuestionId,
            command.OptionId,
            cancellationToken);

        if (existingAnswer != null)
        {
            // Update existing answer
            existingAnswer.UpdateChoice(command.Choice);
            await userAnswerRepo.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Ok(existingAnswer.Id);
        }

        // Create new answer
        var answer = UserAnswer.Create(
            userId: command.UserId,
            quizLectureId: command.QuizLectureId,
            questionId: command.QuestionId,
            optionId: command.OptionId,
            choice: command.Choice);

        await userAnswerRepo.AddAsync(answer, cancellationToken);
        await userAnswerRepo.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Created(answer.Id);
    }
}
