using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TreeEditor.Application.Common.Exceptions;

namespace TreeEditor.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation error"),

            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),

            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),

            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency conflict"),

            DbUpdateException { InnerException: PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation
                } } => (StatusCodes.Status409Conflict, "Concurrency conflict"),

            _ => (StatusCodes.Status500InternalServerError, "Server error")
        };

        LogException(exception, statusCode);

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = GetDetail(exception, statusCode),
                    Extensions = { ["traceId"] = httpContext.TraceIdentifier }
                }
            });
    }

    private void LogException(Exception exception, int statusCode)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception occurred.");

            return;
        }

        logger.LogWarning(exception, "Request failed with status code {StatusCode}.", statusCode);
    }

    private static string GetDetail(Exception exception, int statusCode)
    {
        if (exception is DbUpdateException && statusCode == StatusCodes.Status409Conflict)
        {
            return "The data has been changed by another operation. Reload the affected nodes and try again.";
        }

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            return "An unexpected error occurred.";
        }

        return exception.Message;
    }
}