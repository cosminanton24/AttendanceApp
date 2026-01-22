using FluentValidation;

namespace AttendanceApp.Application.Features.Users.JoinLecture;

public sealed class JoinLectureCommandValidator : AbstractValidator<JoinLectureCommand>
{
    public JoinLectureCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.LectureId)
            .NotEmpty()
            .WithMessage("Lecture ID is required.");
    }
}
