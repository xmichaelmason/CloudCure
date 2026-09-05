using CloudCure.Domain.Workflow;
using Microsoft.AspNetCore.Diagnostics;

namespace CloudCure.Api.ErrorHandling;

/// <summary>
/// Every non-2xx response comes back as structured application/problem+json instead of the
/// old app's pattern of collapsing every failure — validation, not-found, auth, server error —
/// into a bare HTTP 400 with no machine-readable distinction.
/// </summary>
public class ProblemDetailsExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            FormatException => (StatusCodes.Status400BadRequest, "Invalid input"),
            BadHttpRequestException badRequest => (badRequest.StatusCode, "Invalid request"),
            InvalidEncounterTransitionException => (StatusCodes.Status409Conflict, "Invalid workflow transition"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Title = title,
                Status = statusCode,
                Detail = exception.Message,
            },
        });
    }
}
