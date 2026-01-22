using FluentValidation;

namespace AttendanceApp.Application.Features.Users.GetFollowState;

public sealed class GetFollowStateQueryValidator : AbstractValidator<GetFollowStateQuery>
{
    public GetFollowStateQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.TargetId)
            .NotEmpty()
            .WithMessage("Target ID is required.");
    }
}
