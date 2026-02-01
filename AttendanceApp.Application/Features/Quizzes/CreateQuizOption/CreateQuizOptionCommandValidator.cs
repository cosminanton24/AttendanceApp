using FluentValidation;

namespace AttendanceApp.Application.Features.Quizzes.CreateQuizOption;

public sealed class CreateQuizOptionCommandValidator : AbstractValidator<CreateQuizOptionCommand>
{
    public CreateQuizOptionCommandValidator()
    {
        RuleFor(x => x.ProfessorId)
            .NotEmpty()
            .WithMessage("Professor id is required.");

        RuleFor(x => x.QuestionId)
            .NotEmpty()
            .WithMessage("Question id is required.");

        RuleFor(x => x.Text)
            .NotEmpty()
            .WithMessage("Option text is required.")
            .MaximumLength(512)
            .WithMessage("Option text must be at most 512 characters.");

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Order must be at least 1.");
    }
}
