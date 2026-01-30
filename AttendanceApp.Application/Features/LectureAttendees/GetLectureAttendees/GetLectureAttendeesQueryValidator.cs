using FluentValidation;

namespace AttendanceApp.Application.Features.LectureAttendees.GetLectureAttendees;

public sealed class GetLectureAttendeesQueryValidator : AbstractValidator<GetLectureAttendeesQuery>
{
    public GetLectureAttendeesQueryValidator()
    {
        RuleFor(x => x.RequesterId)
            .NotEmpty()
            .WithMessage("Requester id is required.");

        RuleFor(x => x.LectureId)
            .NotEmpty()
            .WithMessage("Lecture id is required.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Page must be 0 or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200)
            .WithMessage("PageSize must be between 1 and 200.");
    }
}
