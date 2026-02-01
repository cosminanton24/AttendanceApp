using FluentValidation;

namespace AttendanceApp.Application.Features.Quizzes.GetQuizzesByLecture;

public sealed class GetQuizzesByLectureQueryValidator : AbstractValidator<GetQuizzesByLectureQuery>
{
    public GetQuizzesByLectureQueryValidator()
    {
        RuleFor(x => x.LectureId)
            .NotEmpty()
            .WithMessage("Lecture id is required.");
    }
}
