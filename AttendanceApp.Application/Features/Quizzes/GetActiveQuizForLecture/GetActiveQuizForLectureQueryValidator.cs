using FluentValidation;

namespace AttendanceApp.Application.Features.Quizzes.GetActiveQuizForLecture;

public sealed class GetActiveQuizForLectureQueryValidator : AbstractValidator<GetActiveQuizForLectureQuery>
{
    public GetActiveQuizForLectureQueryValidator()
    {
        RuleFor(x => x.LectureId)
            .NotEmpty()
            .WithMessage("Lecture id is required.");
    }
}
