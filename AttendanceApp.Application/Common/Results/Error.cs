namespace AttendanceApp.Application.Common.Results;

public sealed record Error(
    string Code,
    string Message,
    ErrorKind Kind = ErrorKind.Failure,
    IReadOnlyDictionary<string, object?>? Metadata = null)
{
    public static Error Unspecified(string? message = null) =>
        new("error.unspecified", message ?? "An unspecified error occurred.", ErrorKind.Failure);

    public static Error Validation(string message, string? code = null, IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code ?? "error.validation", message, ErrorKind.Validation, metadata);

    public static Error NotFound(string message, string? code = null, IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code ?? "error.not_found", message, ErrorKind.NotFound, metadata);

    public static Error Conflict(string message, string? code = null, IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code ?? "error.conflict", message, ErrorKind.Conflict, metadata);

    public override string ToString() => $"{Code}: {Message}";
}