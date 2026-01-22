using FluentValidation;

namespace AttendanceApp.Application.Features.Users.ToggleFollowUser;

public sealed class ToggleFollowUserCommandValidator : AbstractValidator<ToggleFollowUserCommand>
{
    public ToggleFollowUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.TargetId)
            .NotEmpty()
            .WithMessage("Target ID is required.");
    }
}
