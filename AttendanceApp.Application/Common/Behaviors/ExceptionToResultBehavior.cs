using MediatR;
using AttendanceApp.Domain.Common;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using AttendanceApp.Application.Common.Results;

namespace AttendanceApp.Application.Common.Behaviors;

public sealed class ExceptionToResultBehavior<TRequest, TResponse>(
    ILogger<ExceptionToResultBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["Request"] = requestName
        });

        try
        {
            return await next(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (TryHandleException(ex, requestName, out var response))
                return response;

            throw;
        }
    }


    private bool TryHandleException(Exception ex, string requestName, out TResponse response)
    {
        var (error, logLevel) = MapException(ex);

        if (logLevel is not null)
            LogWithLevel(logLevel.Value, ex, requestName);

        return TryBuildFailure(error, out response);
    }


    private static (Error error, LogLevel? logLevel) MapException(Exception ex) =>
        ex switch
        {
            DomainException de => (Error.Validation(de.Message), null),
            ValidationException ve => (Error.Validation(ve.Message), null),
            FluentValidation.ValidationException fve => (Error.Validation(fve.Message), null),

            KeyNotFoundException knf => (Error.NotFound(knf.Message), LogLevel.Warning),

            _ => (Error.Unspecified("An unexpected error occurred."), LogLevel.Error)
        };

    private void LogWithLevel(LogLevel level, Exception ex, string requestName)
    {
        if (!logger.IsEnabled(level))
            return;
            
        switch (level)
        {
            case LogLevel.Warning:
                logger.LogWarning(ex, "Handled exception while handling {Request}", requestName);
                break;
            case LogLevel.Error:
                logger.LogError(ex, "Unhandled exception while handling {Request}", requestName);
                break;
            default:
                logger.LogInformation(ex, "Handled exception while handling {Request}", requestName);
                break;
        }
    }

    private static bool TryBuildFailure(Error error, out TResponse response)
    {
        var t = typeof(TResponse);

        if (t == typeof(Result))
        {
            response = (TResponse)(object)Result.Fail(error);
            return true;
        }

        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failMethod = t.GetMethod("Fail", [typeof(Error)]);
            if (failMethod is not null)
            {
                response = (TResponse)failMethod.Invoke(null, [error])!;
                return true;
            }
        }

        response = default!;
        return false;
    }
}
