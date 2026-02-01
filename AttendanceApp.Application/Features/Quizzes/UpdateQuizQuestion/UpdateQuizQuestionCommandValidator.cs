using FluentValidation;

namespace AttendanceApp.Application.Features.Quizzes.UpdateQuizQuestion;

public sealed class UpdateQuizQuestionCommandValidator : AbstractValidator<UpdateQuizQuestionCommand>
{
    public UpdateQuizQuestionCommandValidator()
    {
        RuleFor(x => x.ProfessorId)
            .NotEmpty()
            .WithMessage("Professor id is required.");

        RuleFor(x => x.QuestionId)
            .NotEmpty()
            .WithMessage("Question id is required.");

        RuleFor(x => x.Text)
            .MaximumLength(2000)
            .When(x => x.Text is not null)
            .WithMessage("Question text must not exceed 2000 characters.");

        RuleFor(x => x.Points)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Points.HasValue)
            .WithMessage("Points must be a non-negative value.");
    }
}
