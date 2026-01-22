using FluentValidation;

namespace AttendanceApp.Application.Features.Lectures.CreateLecture;

public sealed class CreateLectureCommandValidator : AbstractValidator<CreateLectureCommand>
{
    public CreateLectureCommandValidator()
    {
        RuleFor(x => x.ProfessorId)
            .NotEmpty()
            .WithMessage("Professor id is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Lecture name is required.")
            .MaximumLength(100)
            .WithMessage("Lecture name must be at most 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Lecture description is required.")
            .MaximumLength(500)
            .WithMessage("Lecture description must be at most 500 characters.");

        RuleFor(x => x.StartTime)
            .NotEmpty()
            .WithMessage("Start time is required.")
            .Must(startTime => startTime >= DateTime.UtcNow)
            .WithMessage("Start time cannot be in the past.");

        RuleFor(x => x.Duration)
            .NotEmpty()
            .WithMessage("Duration is required.")
            .Must(duration => duration > TimeSpan.Zero)
            .WithMessage("Duration must be greater than zero.");
    }
}
