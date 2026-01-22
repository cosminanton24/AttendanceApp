using FluentValidation;

namespace AttendanceApp.Application.Features.Users.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("User name is required.")
            .MaximumLength(100)
            .WithMessage("User name must be at most 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email address is required.")
            .MaximumLength(255)
            .WithMessage("Email address must be at most 255 characters.")
            .EmailAddress()
            .WithMessage("Email address is not valid.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(255)
            .WithMessage("Password must be at most 255 characters.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("User type is invalid.");
    }
}
