using FluentValidation;

namespace AttendanceApp.Application.Features.Quizzes.CreateQuiz;

public sealed class CreateQuizCommandValidator : AbstractValidator<CreateQuizCommand>
{
    public CreateQuizCommandValidator()
    {
        RuleFor(x => x.ProfessorId)
            .NotEmpty()
            .WithMessage("Professor id is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Quiz name is required.")
            .MaximumLength(256)
            .WithMessage("Quiz name must be at most 256 characters.");

        RuleFor(x => x.Duration)
            .NotEmpty()
            .WithMessage("Duration is required.")
            .Must(d => d > TimeSpan.Zero)
            .WithMessage("Duration must be greater than zero.");
    }
}
