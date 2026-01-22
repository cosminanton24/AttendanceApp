using AttendanceApp.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Api.Common;

public static class ResultActionResultExtensions
{
    public static IActionResult ToActionResult<T>(this ControllerBase controller, Result<T> result)
    {
        if (result.IsSuccess)
        {
            return result.SuccessKind switch
            {
                SuccessKind.Ok => controller.Ok(result.Value),
                SuccessKind.Created => controller.StatusCode(StatusCodes.Status201Created, result.Value),
                SuccessKind.NoContent => controller.NoContent(),
                _ => controller.Ok(result.Value),
            };
        }

        return controller.ToProblem(result.Errors);
    }

    public static IActionResult ToActionResult(this ControllerBase controller, Result result)
    {
        if (result.IsSuccess)
            return controller.NoContent();

        return controller.ToProblem(result.Errors);
    }

    private static ObjectResult ToProblem(this ControllerBase controller, IReadOnlyList<Error> errors)
    {
        var primary = PickPrimary(errors);

        var status = primary.Kind switch
        {
            ErrorKind.Validation   => StatusCodes.Status400BadRequest,
            ErrorKind.NotFound     => StatusCodes.Status404NotFound,
            ErrorKind.Conflict     => StatusCodes.Status409Conflict,
            ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorKind.Forbidden    => StatusCodes.Status403Forbidden,
            _                      => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = primary.Kind.ToString(),
            Detail = primary.Message,
            Type = primary.Code
        };

        if (errors.Count > 1)
        {
            problem.Extensions["errors"] = errors.Select(e => new
            {
                e.Code,
                e.Message,
                Kind = e.Kind.ToString(),
                e.Metadata
            });
        }

        return controller.StatusCode(status, problem);
    }

    private static Error PickPrimary(IReadOnlyList<Error> errors)
    {
        static int Rank(ErrorKind k) => k switch
        {
            ErrorKind.Validation => 5,
            ErrorKind.NotFound => 4,
            ErrorKind.Conflict => 3,
            ErrorKind.Unauthorized => 2,
            ErrorKind.Forbidden => 1,
            _ => 0
        };

        return errors.OrderByDescending(e => Rank(e.Kind)).First();
    }
}
