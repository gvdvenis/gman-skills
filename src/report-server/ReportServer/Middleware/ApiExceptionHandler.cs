using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ReportServer.Middleware;

/// <summary>
/// Maps domain exceptions to RFC 7807 Problem Details responses.
/// <see cref="KeyNotFoundException"/> → 404, <see cref="ArgumentException"/> → 400,
/// all other exceptions return <see langword="false"/> so the default 500 handler runs.
/// </summary>
internal sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found.", "The requested finding was not found."),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request.", "The request contained invalid parameters."),
            _ => (0, null, (string?)null)
        };

        if (title is null)
            return false;

        logger.LogWarning(exception, "Handled domain exception: {ExceptionType}", exception.GetType().Name);

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };

        httpContext.Response.StatusCode = status;

        await problemDetailsService.WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            });

        return true;
    }
}
