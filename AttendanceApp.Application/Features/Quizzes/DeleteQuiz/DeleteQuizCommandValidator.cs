using FluentValidation;

namespace AttendanceApp.Application.Features.Quizzes.DeleteQuiz;

public sealed class DeleteQuizCommandValidator : AbstractValidator<DeleteQuizCommand>
{
    public DeleteQuizCommandValidator()
    {
        RuleFor(x => x.ProfessorId)
            .NotEmpty()
            .WithMessage("Professor id is required.");

        RuleFor(x => x.QuizId)
            .NotEmpty()
            .WithMessage("Quiz id is required.");
    }
}
