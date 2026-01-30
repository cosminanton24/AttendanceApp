using FluentValidation;

namespace AttendanceApp.Application.Features.Users.GetUserInfoBatch;

public sealed class GetUserInfoBatchQueryValidator : AbstractValidator<GetUserInfoBatchQuery>
{
    public GetUserInfoBatchQueryValidator()
    {
        RuleFor(x => x.Ids)
            .NotNull()
            .NotEmpty()
            .WithMessage("At least one user id is required.");

        RuleFor(x => x.Ids)
            .Must(ids => ids is null || ids.Count <= 100)
            .WithMessage("At most 100 user ids are allowed per request.");

        RuleForEach(x => x.Ids)
            .NotEmpty()
            .WithMessage("User id cannot be empty.");
    }
}
