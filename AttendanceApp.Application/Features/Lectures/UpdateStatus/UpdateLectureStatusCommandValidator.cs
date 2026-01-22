using FluentValidation;

namespace AttendanceApp.Application.Features.Lectures.UpdateStatus;

public sealed class UpdateLectureStatusCommandValidator : AbstractValidator<UpdateLectureStatusCommand>
{
    public UpdateLectureStatusCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User id is required.");
            
        RuleFor(x => x.LectureId)
            .NotEmpty()
            .WithMessage("Lecture id is required.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid lecture status.");
    }
}
