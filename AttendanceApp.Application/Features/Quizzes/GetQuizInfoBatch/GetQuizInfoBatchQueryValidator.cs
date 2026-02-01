using FluentValidation;

namespace AttendanceApp.Application.Features.Quizzes.GetQuizInfoBatch;

public sealed class GetQuizInfoBatchQueryValidator : AbstractValidator<GetQuizInfoBatchQuery>
{
    public GetQuizInfoBatchQueryValidator()
    {
        RuleFor(x => x.Ids)
            .NotNull()
            .WithMessage("Ids are required.")
            .Must(ids => ids != null && ids.Count > 0)
            .WithMessage("At least one quiz id is required.");
    }
}
