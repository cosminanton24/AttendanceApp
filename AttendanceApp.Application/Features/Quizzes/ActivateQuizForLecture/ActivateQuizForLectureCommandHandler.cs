using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Quizzes;
using AttendanceApp.Domain.Repositories;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Application.Features.Quizzes.ActivateQuizForLecture;

public class ActivateQuizForLectureCommandHandler(
    IQuizLectureRepository quizLectureRepo,
    IQuizRepository quizRepo,
    ILectureRepository lectureRepo,
    IUserRepository userRepo)
    : IRequestHandler<ActivateQuizForLectureCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ActivateQuizForLectureCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepo.GetByIdAsync(command.ProfessorId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with id {command.ProfessorId} found.");

        if (user.Type == UserType.Student)
        {
            throw new ValidationException("User is not authorized to activate quizzes.");
        }

        var lecture = await lectureRepo.GetByIdAsync(command.LectureId, cancellationToken)
            ?? throw new KeyNotFoundException($"No lecture with id {command.LectureId} found.");

        if (lecture.ProfessorId != command.ProfessorId)
        {
            throw new ValidationException("You can only activate quizzes for your own lectures.");
        }

        if (lecture.Status != LectureStatus.InProgress)
        {
            throw new ValidationException("Can only activate quizzes for in progress lectures.");
        }

        var quiz = await quizRepo.GetByIdAsync(command.QuizId, cancellationToken)
            ?? throw new KeyNotFoundException($"No quiz with id {command.QuizId} found.");

        if (quiz.ProfessorId != command.ProfessorId)
        {
            throw new ValidationException("You can only activate your own quizzes.");
        }

        var now = DateTime.UtcNow;
        if (await quizLectureRepo.HasActiveQuizForLectureAsync(command.LectureId, now, cancellationToken))
        {
            throw new ValidationException("There is already an active quiz for this lecture.");
        }

        var quizLecture = QuizLecture.Create(
            lectureId: command.LectureId,
            quizId: command.QuizId,
            createdAtUtc: now,
            quizDuration: quiz.Duration
        );

        await quizLectureRepo.AddAsync(quizLecture, cancellationToken);
        await quizLectureRepo.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Created(quizLecture.Id);
    }
}
