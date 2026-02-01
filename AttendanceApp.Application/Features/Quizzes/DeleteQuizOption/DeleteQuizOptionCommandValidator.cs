using FluentValidation;

namespace AttendanceApp.Application.Features.Quizzes.DeleteQuizOption;

public sealed class DeleteQuizOptionCommandValidator : AbstractValidator<DeleteQuizOptionCommand>
{
    public DeleteQuizOptionCommandValidator()
    {
        RuleFor(x => x.ProfessorId)
            .NotEmpty()
            .WithMessage("Professor id is required.");

        RuleFor(x => x.OptionId)
            .NotEmpty()
            .WithMessage("Option id is required.");
    }
}
