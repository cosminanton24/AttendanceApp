using AttendanceApp.Application.Common.Results;

namespace AttendanceApp.Tests.Application.Common.Results
{
    public sealed class ResultTests
    {
        // -------------------------
        // Success factories
        // -------------------------

        [Fact]
        public void Ok_Default_IsSuccess_True_SuccessKind_NoContent_And_EmptyErrors()
        {
            var r = Result.Ok();

            Assert.True(r.IsSuccess);
            Assert.False(r.IsFailure);
            Assert.Equal(SuccessKind.NoContent, r.SuccessKind); // Ok() => NoContent
            Assert.NotNull(r.Errors);
            Assert.Empty(r.Errors);
        }

        [Fact]
        public void Ok_With_SuccessKind_Sets_SuccessKind_And_EmptyErrors()
        {
            var r = Result.Ok(SuccessKind.Created);

            Assert.True(r.IsSuccess);
            Assert.Equal(SuccessKind.Created, r.SuccessKind);   // Ok(successKind)
            Assert.Empty(r.Errors);
        }

        [Fact]
        public void NoContent_IsSuccess_And_SuccessKind_NoContent()
        {
            var r = Result.NoContent();

            Assert.True(r.IsSuccess);
            Assert.Equal(SuccessKind.NoContent, r.SuccessKind);
        }

        [Fact]
        public void Created_IsSuccess_And_SuccessKind_Created()
        {
            var r = Result.Created();

            Assert.True(r.IsSuccess);
            Assert.Equal(SuccessKind.Created, r.SuccessKind);
        }

        // -------------------------
        // Fail factories + normalization
        // -------------------------

        [Fact]
        public void Fail_With_Single_Error_Sets_IsFailure_And_Stores_Error()
        {
            var err = new Error("code.x", "boom", ErrorKind.Failure);
            var r = Result.Fail(err);

            Assert.True(r.IsFailure);
            Assert.False(r.IsSuccess);
            Assert.Equal(SuccessKind.Ok, r.SuccessKind);        // Fail uses SuccessKind.Ok
            Assert.Single(r.Errors);
            Assert.Equal(err, r.Errors[0]);
        }

        [Fact]
        public void Fail_Params_Null_Normalizes_To_Unspecified_Error()
        {
            Error[]? errors = null;

            var r = Result.Fail(errors!);

            Assert.True(r.IsFailure);
            Assert.Single(r.Errors);
            Assert.Equal("error.unspecified", r.Errors[0].Code); // Error.Unspecified
            Assert.Equal(ErrorKind.Failure, r.Errors[0].Kind);
        }

        [Fact]
        public void Fail_Params_Empty_Normalizes_To_Unspecified_Error()
        {
            var r = Result.Fail(Array.Empty<Error>());

            Assert.True(r.IsFailure);
            Assert.Single(r.Errors);                             // NormalizeErrors for empty
            Assert.Equal("error.unspecified", r.Errors[0].Code);
        }

        [Fact]
        public void Fail_List_Null_Normalizes_To_Unspecified_Error()
        {
            IReadOnlyList<Error>? errors = null;

            var r = Result.Fail(errors!);

            Assert.True(r.IsFailure);
            Assert.Single(r.Errors);
            Assert.Equal("error.unspecified", r.Errors[0].Code);
        }

        [Fact]
        public void Fail_List_Empty_Normalizes_To_Unspecified_Error()
        {
            IReadOnlyList<Error> errors = [];

            var r = Result.Fail(errors);

            Assert.True(r.IsFailure);
            Assert.Single(r.Errors);
            Assert.Equal("error.unspecified", r.Errors[0].Code);
        }

        [Fact]
        public void Fail_List_NonEmpty_Preserves_Errors()
        {
            var e1 = new Error("a", "A", ErrorKind.Failure);
            var e2 = new Error("b", "B", ErrorKind.Validation);
            IReadOnlyList<Error> errors = new[] { e1, e2 };

            var r = Result.Fail(errors);

            Assert.True(r.IsFailure);
            Assert.Equal(2, r.Errors.Count);
            Assert.Equal(e1, r.Errors[0]);
            Assert.Equal(e2, r.Errors[1]);
        }

        // -------------------------
        // ToResult<T>
        // -------------------------

        [Fact]
        public void ToResult_Success_Produces_ResultT_Ok_With_Same_SuccessKind()
        {
            var r = Result.Ok(SuccessKind.Created);

            var rt = r.ToResult(123);

            Assert.True(rt.IsSuccess);
            Assert.Equal(SuccessKind.Created, rt.SuccessKind);
            Assert.Equal(123, rt.Value);
        }

        [Fact]
        public void ToResult_Failure_Produces_ResultT_Fail_With_Same_Errors()
        {
            var e = new Error("code.x", "boom", ErrorKind.Failure);
            var r = Result.Fail(e);

            var rt = r.ToResult(123);

            Assert.True(rt.IsFailure); 
            Assert.Single(rt.Errors);
            Assert.Equal(e, rt.Errors[0]);
        }

        // -------------------------
        // Match + argument guards
        // -------------------------

        [Fact]
        public void Match_Throws_When_OnSuccess_Is_Null()
        {
            var r = Result.Ok();

            Assert.Throws<ArgumentNullException>(() =>
                r.Match<string>(null!, _ => "fail"));
        }

        [Fact]
        public void Match_Throws_When_OnFailure_Is_Null()
        {
            var r = Result.Ok();

            Assert.Throws<ArgumentNullException>(() =>
                r.Match<string>(() => "ok", null!));
        }

        [Fact]
        public void Match_OnSuccess_Runs_When_Success()
        {
            var r = Result.Created();

            var value = r.Match(
                onSuccess: () => "YES",
                onFailure: _ => "NO");

            Assert.Equal("YES", value);
        }

        [Fact]
        public void Match_OnFailure_Runs_When_Failure()
        {
            var r = Result.Fail(new Error("c", "msg", ErrorKind.Failure));

            var value = r.Match(
                onSuccess: () => "YES",
                onFailure: errs => $"NO:{errs.Count}");

            Assert.Equal("NO:1", value);
        }
    }
}
