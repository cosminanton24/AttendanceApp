using FluentValidation;

namespace AttendanceApp.Application.Features.Quizzes.UpdateQuiz;

public sealed class UpdateQuizCommandValidator : AbstractValidator<UpdateQuizCommand>
{
    public UpdateQuizCommandValidator()
    {
        RuleFor(x => x.ProfessorId)
            .NotEmpty()
            .WithMessage("Professor id is required.");

        RuleFor(x => x.QuizId)
            .NotEmpty()
            .WithMessage("Quiz id is required.");

        RuleFor(x => x.Name)
            .MaximumLength(256)
            .When(x => x.Name is not null)
            .WithMessage("Quiz name must not exceed 256 characters.");
    }
}
