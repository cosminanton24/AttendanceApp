using FluentValidation;

namespace AttendanceApp.Application.Features.Lectures.GetProfessorLectures;

public sealed class GetProfessorLecturesQueryValidator : AbstractValidator<GetProfessorLecturesQuery>
{
    public GetProfessorLecturesQueryValidator()
    {
        RuleFor(x => x.ProfessorId)
            .NotEmpty()
            .WithMessage("Professor id is required.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Page number must be greater than or equal to 0.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0.");
    }
}
