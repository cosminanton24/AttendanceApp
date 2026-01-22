using FluentValidation;

namespace AttendanceApp.Application.Features.Lectures.GetStudentLectures;

public sealed class GetStudentLecturesQueryValidator : AbstractValidator<GetStudentLecturesQuery>
{
    public GetStudentLecturesQueryValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("Student id is required.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Page number must be greater than or equal to 0.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0.");
    }
}
