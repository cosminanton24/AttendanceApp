using AttendanceApp.Api.Common;
using AttendanceApp.Application.Common.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.UnitTests.Api;

public sealed class ResultActionResultExtensionsTests
{
    private static TestController NewController()
    {
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    // -------------------------
    // Success: Result<T>
    // -------------------------

    [Fact]
    public void ToActionResult_T_Success_Ok_Returns_OkObjectResult_With_Value()
    {
        var controller = NewController();
        var result = Result<string>.Ok("hello", SuccessKind.Ok);

        var action = controller.ToActionResult(result);

        var ok = Assert.IsType<OkObjectResult>(action);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        Assert.Equal("hello", ok.Value);
    }

    [Fact]
    public void ToActionResult_T_Success_Created_Returns_201_ObjectResult_With_Value()
    {
        var controller = NewController();
        var result = Result<int>.Ok(42, SuccessKind.Created);

        var action = controller.ToActionResult(result);

        var obj = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status201Created, obj.StatusCode);
        Assert.Equal(42, obj.Value);
    }

    [Fact]
    public void ToActionResult_T_Success_NoContent_Returns_NoContentResult()
    {
        var controller = NewController();
        var result = Result<string>.NoContent();

        var action = controller.ToActionResult(result);

        Assert.IsType<NoContentResult>(action);
    }

    // -------------------------
    // Success: non-generic Result
    // -------------------------

    [Fact]
    public void ToActionResult_NonGeneric_Success_Returns_OkResult()
    {
        var controller = NewController();
        var result = Result.Ok();

        var action = controller.ToActionResult(result);

        var ok = Assert.IsType<OkResult>(action);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    // -------------------------
    // Failures: status mapping
    // -------------------------

    [Theory]
    [InlineData(ErrorKind.Validation,   StatusCodes.Status400BadRequest)]
    [InlineData(ErrorKind.NotFound,     StatusCodes.Status404NotFound)]
    [InlineData(ErrorKind.Conflict,     StatusCodes.Status409Conflict)]
    [InlineData(ErrorKind.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorKind.Forbidden,    StatusCodes.Status403Forbidden)]
    [InlineData(ErrorKind.Failure,      StatusCodes.Status500InternalServerError)]
    public void ToActionResult_T_Failure_Maps_ErrorKind_To_StatusCode(ErrorKind kind, int expectedStatus)
    {
        var controller = NewController();
        var err = new Error("code.x", "boom", kind);
        var result = Result<string>.Fail(err);

        var action = controller.ToActionResult(result);

        var obj = Assert.IsType<ObjectResult>(action);
        Assert.Equal(expectedStatus, obj.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(obj.Value);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal(kind.ToString(), problem.Title);
        Assert.Equal("boom", problem.Detail);
        Assert.Equal("code.x", problem.Type);
    }

    // -------------------------
    // Primary error selection (ranking)
    // Validation > NotFound > Conflict > Unauthorized > Forbidden > Failure
    // -------------------------

    [Fact]
    public void ToActionResult_T_Failure_Picks_Primary_Error_By_Rank()
    {
        var controller = NewController();

        var errors = new[]
        {
            new Error("code.failure", "failure msg", ErrorKind.Failure),
            new Error("code.forbidden", "forbidden msg", ErrorKind.Forbidden),
            new Error("code.validation", "validation msg", ErrorKind.Validation),
            new Error("code.notfound", "notfound msg", ErrorKind.NotFound),
        };

        var result = Result<string>.Fail(errors);

        var action = controller.ToActionResult(result);

        var obj = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status400BadRequest, obj.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(obj.Value);
        Assert.Equal("Validation", problem.Title);
        Assert.Equal("validation msg", problem.Detail);
        Assert.Equal("code.validation", problem.Type);
    }

    // -------------------------
    // ProblemDetails extensions
    // -------------------------

    [Fact]
    public void ToActionResult_T_Failure_Single_Error_Does_Not_Add_Errors_Extension()
    {
        var controller = NewController();
        var result = Result<string>.Fail(new Error("code.one", "only one", ErrorKind.NotFound));

        var action = controller.ToActionResult(result);

        var obj = Assert.IsType<ObjectResult>(action);
        var problem = Assert.IsType<ProblemDetails>(obj.Value);

        Assert.False(problem.Extensions.ContainsKey("errors"));
    }

    [Fact]
    public void ToActionResult_T_Failure_Multiple_Errors_Adds_Errors_Extension_With_Details()
    {
        var controller = NewController();

        var errors = new[]
        {
            new Error("code.a", "msg a", ErrorKind.Conflict, new Dictionary<string, object?> { ["field"] = "a" }),
            new Error("code.b", "msg b", ErrorKind.Validation, new Dictionary<string, object?> { ["field"] = "b" }),
        };

        var result = Result<string>.Fail(errors);

        var action = controller.ToActionResult(result);

        var obj = Assert.IsType<ObjectResult>(action);
        var problem = Assert.IsType<ProblemDetails>(obj.Value);

        Assert.True(problem.Extensions.TryGetValue("errors", out var ext));
        Assert.NotNull(ext);

        // Extension is created via LINQ Select of anonymous objects => treat as IEnumerable<object>
        var items = Assert.IsType<IEnumerable<object>>(ext, exactMatch: false);
        var list = items.ToList();
        Assert.Equal(2, list.Count);

        // We can verify shape by reflecting properties of the anonymous objects
        static object? GetProp(object o, string name) =>
            o.GetType().GetProperty(name)?.GetValue(o);

        Assert.Equal("code.a", GetProp(list[0], "Code"));
        Assert.Equal("msg a",  GetProp(list[0], "Message"));
        Assert.Equal("Conflict", GetProp(list[0], "Kind"));
        Assert.NotNull(GetProp(list[0], "Metadata"));

        Assert.Equal("code.b", GetProp(list[1], "Code"));
        Assert.Equal("msg b",  GetProp(list[1], "Message"));
        Assert.Equal("Validation", GetProp(list[1], "Kind"));
        Assert.NotNull(GetProp(list[1], "Metadata"));
    }

    private sealed class TestController : ControllerBase { }
}
