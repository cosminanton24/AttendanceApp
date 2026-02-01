using FluentValidation;

namespace AttendanceApp.Application.Features.Quizzes.ActivateQuizForLecture;

public sealed class ActivateQuizForLectureCommandValidator : AbstractValidator<ActivateQuizForLectureCommand>
{
    public ActivateQuizForLectureCommandValidator()
    {
        RuleFor(x => x.ProfessorId)
            .NotEmpty()
            .WithMessage("Professor id is required.");

        RuleFor(x => x.LectureId)
            .NotEmpty()
            .WithMessage("Lecture id is required.");

        RuleFor(x => x.QuizId)
            .NotEmpty()
            .WithMessage("Quiz id is required.");
    }
}
