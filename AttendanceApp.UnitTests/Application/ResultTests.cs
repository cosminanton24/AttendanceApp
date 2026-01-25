using AttendanceApp.Application.Common.Results;

namespace AttendanceApp.Tests.Application.Common.Results
{
    public sealed class ErrorTests
    {
        [Fact]
        public void Unspecified_DefaultMessage_And_DefaultKind()
        {
            var e = Error.Unspecified();

            Assert.Equal("error.unspecified", e.Code);
            Assert.Equal("An unspecified error occurred.", e.Message);
            Assert.Equal(ErrorKind.Failure, e.Kind);
            Assert.Null(e.Metadata);
        }

        [Fact]
        public void Unspecified_CustomMessage()
        {
            var e = Error.Unspecified("custom");

            Assert.Equal("error.unspecified", e.Code);
            Assert.Equal("custom", e.Message);
            Assert.Equal(ErrorKind.Failure, e.Kind);
        }

        [Fact]
        public void Validation_DefaultCode_And_Metadata()
        {
            var md = new Dictionary<string, object?> { ["field"] = "name" };
            var e = Error.Validation("bad", metadata: md);

            Assert.Equal("error.validation", e.Code);
            Assert.Equal("bad", e.Message);
            Assert.Equal(ErrorKind.Validation, e.Kind);
            Assert.Same(md, e.Metadata);
        }

        [Fact]
        public void Validation_CustomCode()
        {
            var e = Error.Validation("bad", code: "v.custom");

            Assert.Equal("v.custom", e.Code);
            Assert.Equal(ErrorKind.Validation, e.Kind);
        }

        [Fact]
        public void NotFound_DefaultCode_And_Metadata()
        {
            var md = new Dictionary<string, object?> { ["id"] = 1 };
            var e = Error.NotFound("missing", metadata: md);

            Assert.Equal("error.not_found", e.Code);
            Assert.Equal("missing", e.Message);
            Assert.Equal(ErrorKind.NotFound, e.Kind);
            Assert.Same(md, e.Metadata);
        }

        [Fact]
        public void NotFound_CustomCode()
        {
            var e = Error.NotFound("missing", code: "nf.custom");

            Assert.Equal("nf.custom", e.Code);
            Assert.Equal(ErrorKind.NotFound, e.Kind);
        }

        [Fact]
        public void Conflict_DefaultCode_And_Metadata()
        {
            var md = new Dictionary<string, object?> { ["key"] = "k" };
            var e = Error.Conflict("dup", metadata: md);

            Assert.Equal("error.conflict", e.Code);
            Assert.Equal("dup", e.Message);
            Assert.Equal(ErrorKind.Conflict, e.Kind);
            Assert.Same(md, e.Metadata);
        }

        [Fact]
        public void Conflict_CustomCode()
        {
            var e = Error.Conflict("dup", code: "c.custom");

            Assert.Equal("c.custom", e.Code);
            Assert.Equal(ErrorKind.Conflict, e.Kind);
        }

        [Fact]
        public void ToString_Formats_Code_And_Message()
        {
            var e = new Error("x", "y", ErrorKind.Failure);
            Assert.Equal("x: y", e.ToString());
        }
    }

    public sealed class ResultNonGenericTests
    {
        [Fact]
        public void Ok_Default_IsSuccess_And_NoContent()
        {
            var r = Result.Ok();

            Assert.True(r.IsSuccess);
            Assert.False(r.IsFailure);
            Assert.Equal(SuccessKind.NoContent, r.SuccessKind);
            Assert.NotNull(r.Errors);
            Assert.Empty(r.Errors);

            Assert.Equal("Ok[NoContent]", r.ToString());
        }

        [Fact]
        public void Ok_With_SuccessKind()
        {
            var r = Result.Ok(SuccessKind.Created);

            Assert.True(r.IsSuccess);
            Assert.Equal(SuccessKind.Created, r.SuccessKind);
            Assert.Empty(r.Errors);

            Assert.Equal("Ok[Created]", r.ToString());
        }

        [Fact]
        public void NoContent_And_Created_Are_Ok_Shortcuts()
        {
            Assert.Equal(SuccessKind.NoContent, Result.NoContent().SuccessKind);
            Assert.Equal(SuccessKind.Created, Result.Created().SuccessKind);
        }

        [Fact]
        public void Fail_With_Single_Error_Preserves_Error_And_ToString()
        {
            var e = new Error("a", "A", ErrorKind.Failure);
            var r = Result.Fail(e);

            Assert.True(r.IsFailure);
            Assert.Equal(SuccessKind.Ok, r.SuccessKind);
            Assert.Single(r.Errors);
            Assert.Equal(e, r.Errors[0]);
            Assert.Equal("Fail(a: A)", r.ToString());
        }

        [Fact]
        public void Fail_Params_Null_Normalizes_To_Unspecified()
        {
            Error[]? errors = null;
            var r = Result.Fail(errors!);

            Assert.True(r.IsFailure);
            Assert.Single(r.Errors);
            Assert.Equal("error.unspecified", r.Errors[0].Code);
        }

        [Fact]
        public void Fail_Params_Empty_Normalizes_To_Unspecified()
        {
            var r = Result.Fail(Array.Empty<Error>());

            Assert.True(r.IsFailure);
            Assert.Single(r.Errors);
            Assert.Equal("error.unspecified", r.Errors[0].Code);
        }

        [Fact]
        public void Fail_Params_NonEmpty_Does_Not_Normalize()
        {
            var e1 = new Error("a", "A");
            var e2 = new Error("b", "B");
            var r = Result.Fail(e1, e2);

            Assert.Equal(2, r.Errors.Count);
            Assert.Equal(e1, r.Errors[0]);
            Assert.Equal(e2, r.Errors[1]);
        }

        [Fact]
        public void Fail_List_Null_Normalizes_To_Unspecified()
        {
            IReadOnlyList<Error>? errors = null;
            var r = Result.Fail(errors!);

            Assert.True(r.IsFailure);
            Assert.Single(r.Errors);
            Assert.Equal("error.unspecified", r.Errors[0].Code);
        }

        [Fact]
        public void Fail_List_Empty_Normalizes_To_Unspecified()
        {
            IReadOnlyList<Error> errors = Array.Empty<Error>();
            var r = Result.Fail(errors);

            Assert.True(r.IsFailure);
            Assert.Single(r.Errors);
            Assert.Equal("error.unspecified", r.Errors[0].Code);
        }

        [Fact]
        public void Fail_List_NonEmpty_Does_Not_Normalize()
        {
            IReadOnlyList<Error> errors = new[] { new Error("x", "y"), new Error("p", "q") };
            var r = Result.Fail(errors);

            Assert.Equal(2, r.Errors.Count);
            Assert.Equal("x", r.Errors[0].Code);
            Assert.Equal("p", r.Errors[1].Code);
        }

        [Fact]
        public void ToResult_Success_Uses_Same_SuccessKind()
        {
            var r = Result.Ok(SuccessKind.Created);

            var rt = r.ToResult(123);

            Assert.True(rt.IsSuccess);
            Assert.Equal(SuccessKind.Created, rt.SuccessKind);
            Assert.Equal(123, rt.Value);
        }

        [Fact]
        public void ToResult_Failure_Propagates_Errors()
        {
            var e = new Error("x", "y");
            var r = Result.Fail(e);

            var rt = r.ToResult(123);

            Assert.True(rt.IsFailure);
            Assert.Single(rt.Errors);
            Assert.Equal(e, rt.Errors[0]);
        }

        [Fact]
        public void Match_Throws_On_Nulls()
        {
            var ok = Result.Ok();
            Assert.Throws<ArgumentNullException>(() => ok.Match<string>(null!, _ => "fail"));
            Assert.Throws<ArgumentNullException>(() => ok.Match<string>(() => "ok", null!));
        }

        [Fact]
        public void Match_Runs_Success_Or_Failure_Branch()
        {
            var ok = Result.Ok(SuccessKind.NoContent);
            var fail = Result.Fail(new Error("c", "msg"));

            Assert.Equal("YES", ok.Match(() => "YES", _ => "NO"));
            Assert.Equal("NO:1", fail.Match(() => "YES", errs => $"NO:{errs.Count}"));
        }
    }

    public sealed class ResultGenericTests
    {
        [Fact]
        public void Ok_Null_Value_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Result<string>.Ok(null!));
            Assert.Throws<ArgumentNullException>(() => Result<string>.Ok(null!, SuccessKind.Ok));
        }

        [Fact]
        public void Ok_And_Created_And_NoContent_Work()
        {
            var ok = Result<int>.Ok(7);
            Assert.True(ok.IsSuccess);
            Assert.Equal(SuccessKind.Ok, ok.SuccessKind);
            Assert.Equal(7, ok.Value);

            var created = Result<int>.Created(9);
            Assert.True(created.IsSuccess);
            Assert.Equal(SuccessKind.Created, created.SuccessKind);
            Assert.Equal(9, created.Value);

            var nc = Result<int>.NoContent();
            Assert.True(nc.IsSuccess);
            Assert.Equal(SuccessKind.NoContent, nc.SuccessKind);
        }

        [Fact]
        public void OkNullable_Allows_Null_And_ToString_Uses_null()
        {
            var r = Result<string>.OkNullable(null, SuccessKind.Ok);

            Assert.True(r.IsSuccess);
            Assert.Null(r.TryGetValue(out var v) ? v : null); // forces reading via TryGet helper below
            Assert.Equal("Ok(null)[Ok]", r.ToString());
        }

        [Fact]
        public void Value_On_Failure_Throws()
        {
            var r = Result<int>.Fail(new Error("x", "y"));

            Assert.True(r.IsFailure);
            Assert.Throws<InvalidOperationException>(() => _ = r.Value);
        }

        [Fact]
        public void Fail_Overloads_Normalize_Null_And_Empty()
        {
            // Fail(Error)
            var e = new Error("a", "A");
            var f1 = Result<int>.Fail(e);
            Assert.Single(f1.Errors);
            Assert.Equal(e, f1.Errors[0]);

            // Fail(params) null -> unspecified
            Error[]? arr = null;
            var f2 = Result<int>.Fail(arr!);
            Assert.Single(f2.Errors);
            Assert.Equal("error.unspecified", f2.Errors[0].Code);

            // Fail(params) empty -> unspecified
            var f3 = Result<int>.Fail(Array.Empty<Error>());
            Assert.Single(f3.Errors);
            Assert.Equal("error.unspecified", f3.Errors[0].Code);

            // Fail(list) null -> unspecified
            IReadOnlyList<Error>? list = null;
            var f4 = Result<int>.Fail(list!);
            Assert.Single(f4.Errors);
            Assert.Equal("error.unspecified", f4.Errors[0].Code);

            // Fail(list) empty -> unspecified
            IReadOnlyList<Error> emptyList = Array.Empty<Error>();
            var f5 = Result<int>.Fail(emptyList);
            Assert.Single(f5.Errors);
            Assert.Equal("error.unspecified", f5.Errors[0].Code);
        }

        [Fact]
        public void Map_Covers_Success_And_Failure_And_Throws_On_Null_Mapper()
        {
            var ok = Result<int>.Ok(2);
            var fail = Result<int>.Fail(new Error("x", "y"));

            Assert.Throws<ArgumentNullException>(() => ok.Map<string>(null!));

            var mappedOk = ok.Map(x => (string?)("v" + x)); // returns nullable => OkNullable path
            Assert.True(mappedOk.IsSuccess);
            Assert.Equal("v2", mappedOk.Value);

            var mappedFail = fail.Map(x => x.ToString());
            Assert.True(mappedFail.IsFailure);
            Assert.Equal("x", mappedFail.Errors[0].Code);
        }

        [Fact]
        public void Bind_Covers_Success_And_Failure_And_Throws_On_Null_Binder()
        {
            var ok = Result<int>.Ok(2);
            var fail = Result<int>.Fail(new Error("x", "y"));

            Assert.Throws<ArgumentNullException>(() => ok.Bind<string>(null!));

            var boundOk = ok.Bind(x => Result<string>.Ok("n" + x));
            Assert.True(boundOk.IsSuccess);
            Assert.Equal("n2", boundOk.Value);

            var boundFail = fail.Bind(x => Result<string>.Ok("never"));
            Assert.True(boundFail.IsFailure);
            Assert.Equal("x", boundFail.Errors[0].Code);
        }

        [Fact]
        public void Match_Covers_Success_And_Failure_And_Throws_On_Nulls()
        {
            var ok = Result<int>.Ok(3);
            var fail = Result<int>.Fail(new Error("x", "y"));

            Assert.Throws<ArgumentNullException>(() => ok.Match<string>(null!, _ => "F"));
            Assert.Throws<ArgumentNullException>(() => ok.Match<string>(_ => "S", null!));

            Assert.Equal("S3", ok.Match(v => "S" + v, _ => "F"));
            Assert.Equal("F1", fail.Match(_ => "S", errs => "F" + errs.Count));
        }

        [Fact]
        public void Tap_And_TapFailure_Cover_Success_And_Failure_And_Throws_On_Nulls()
        {
            var ok = Result<int>.Ok(5);
            var fail = Result<int>.Fail(new Error("x", "y"));

            Assert.Throws<ArgumentNullException>(() => ok.Tap(null!));
            Assert.Throws<ArgumentNullException>(() => ok.TapFailure(null!));

            int seen = 0;
            ok.Tap(v => seen = v);
            Assert.Equal(5, seen);

            // Tap on failure should not execute
            seen = 0;
            fail.Tap(v => seen = 123);
            Assert.Equal(0, seen);

            int failureCount = 0;
            fail.TapFailure(errs => failureCount = errs.Count);
            Assert.Equal(1, failureCount);

            // TapFailure on success should not execute
            failureCount = 0;
            ok.TapFailure(_ => failureCount = 999);
            Assert.Equal(0, failureCount);
        }

        [Fact]
        public void TryGetValue_Covers_Success_And_Failure()
        {
            var ok = Result<int>.Ok(10);
            Assert.True(ok.TryGetValue(out var v1));
            Assert.Equal(10, v1);

            var fail = Result<int>.Fail(new Error("x", "y"));
            Assert.False(fail.TryGetValue(out var v2));
            Assert.Equal(default, v2); // _value is default(int) on failure
        }

        [Fact]
        public void ToString_Covers_Success_NonNull_And_Failure()
        {
            var ok = Result<string>.Ok("hi", SuccessKind.Ok);
            Assert.Equal("Ok(hi)[Ok]", ok.ToString());

            var fail = Result<string>.Fail(new Error("a", "A"), new Error("b", "B"));
            Assert.Equal("Fail(a: A, b: B)", fail.ToString());
        }

        [Fact]
        public void Implicit_Operator_Creates_Ok()
        {
            Result<int> r = 7; // implicit operator
            Assert.True(r.IsSuccess);
            Assert.Equal(7, r.Value);
        }


    }
}
