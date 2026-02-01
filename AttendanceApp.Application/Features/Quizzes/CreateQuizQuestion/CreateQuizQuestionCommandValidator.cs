using FluentValidation;

namespace AttendanceApp.Application.Features.Quizzes.CreateQuizQuestion;

public sealed class CreateQuizQuestionCommandValidator : AbstractValidator<CreateQuizQuestionCommand>
{
    public CreateQuizQuestionCommandValidator()
    {
        RuleFor(x => x.ProfessorId)
            .NotEmpty()
            .WithMessage("Professor id is required.");

        RuleFor(x => x.QuizId)
            .NotEmpty()
            .WithMessage("Quiz id is required.");

        RuleFor(x => x.Text)
            .NotEmpty()
            .WithMessage("Question text is required.")
            .MaximumLength(1024)
            .WithMessage("Question text must be at most 1024 characters.");

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Order must be at least 1.");

        RuleFor(x => x.Points)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Points.HasValue)
            .WithMessage("Points cannot be negative.");
    }
}
