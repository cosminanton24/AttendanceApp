using FluentValidation;

namespace AttendanceApp.Application.Features.Lectures.DeleteLecture;

public sealed class DeleteLectureCommandValidator : AbstractValidator<DeleteLectureCommand>
{
    public DeleteLectureCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User id is required.");

        RuleFor(x => x.LectureId)
            .NotEmpty()
            .WithMessage("Lecture id is required.");
    }
}
