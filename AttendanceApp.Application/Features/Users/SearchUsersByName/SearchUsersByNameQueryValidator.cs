using FluentValidation;

namespace AttendanceApp.Application.Features.Users.SearchUsersByName;

public sealed class SearchUsersByNameQueryValidator : AbstractValidator<SearchUsersByNameQuery>
{
    public SearchUsersByNameQueryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(100)
            .WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Page must be 0 or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200)
            .WithMessage("PageSize must be between 1 and 200.");
    }
}
