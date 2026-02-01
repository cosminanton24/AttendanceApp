using FluentValidation;

namespace AttendanceApp.Application.Features.Quizzes.GetQuizzesByProfessor;

public sealed class GetQuizzesByProfessorQueryValidator : AbstractValidator<GetQuizzesByProfessorQuery>
{
    public GetQuizzesByProfessorQueryValidator()
    {
        RuleFor(x => x.ProfessorId)
            .NotEmpty()
            .WithMessage("Professor id is required.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Page number must be at least 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");
    }
}
