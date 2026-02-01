using FluentValidation;

namespace AttendanceApp.Application.Features.Quizzes.UpdateQuizOption;

public sealed class UpdateQuizOptionCommandValidator : AbstractValidator<UpdateQuizOptionCommand>
{
    public UpdateQuizOptionCommandValidator()
    {
        RuleFor(x => x.ProfessorId)
            .NotEmpty()
            .WithMessage("Professor id is required.");

        RuleFor(x => x.OptionId)
            .NotEmpty()
            .WithMessage("Option id is required.");

        RuleFor(x => x.Text)
            .MaximumLength(1000)
            .When(x => x.Text is not null)
            .WithMessage("Option text must not exceed 1000 characters.");
    }
}
