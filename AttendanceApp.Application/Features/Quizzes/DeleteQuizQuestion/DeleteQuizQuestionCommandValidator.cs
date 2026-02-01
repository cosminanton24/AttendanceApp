using FluentValidation;

namespace AttendanceApp.Application.Features.Quizzes.DeleteQuizQuestion;

public sealed class DeleteQuizQuestionCommandValidator : AbstractValidator<DeleteQuizQuestionCommand>
{
    public DeleteQuizQuestionCommandValidator()
    {
        RuleFor(x => x.ProfessorId)
            .NotEmpty()
            .WithMessage("Professor id is required.");

        RuleFor(x => x.QuestionId)
            .NotEmpty()
            .WithMessage("Question id is required.");
    }
}
