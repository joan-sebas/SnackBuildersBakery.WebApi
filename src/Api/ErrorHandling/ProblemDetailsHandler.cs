using Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Api.ErrorHandling;

internal sealed class ProblemDetailsHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var mapping = ProblemDetailsMapping.FromException(exception);
        httpContext.Response.StatusCode = mapping.StatusCode;

        var problemDetails = new ProblemDetails
        {
            Type = mapping.Type,
            Title = mapping.Title,
            Detail = mapping.Detail,
            Status = mapping.StatusCode
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });

        return true;
    }
}

internal readonly record struct ProblemDetailsMapping(int StatusCode, string Type, string Title, string Detail)
{
    public static ProblemDetailsMapping FromException(Exception exception)
    {
        return exception switch
        {
            OrderAlreadyPaidError => Conflict(exception.Message),
            InvalidOrderItemTransitionError => Conflict(exception.Message),
            InvalidPaymentTransitionError => Conflict(exception.Message),
            InvalidOvenSlotTransitionError => Conflict(exception.Message),
            DomainError => UnprocessableEntity(exception.Message),
            ArgumentException => BadRequest(exception.Message),
            BadHttpRequestException => BadRequest(exception.Message),
            _ => InternalServerError()
        };
    }

    private static ProblemDetailsMapping BadRequest(string detail)
    {
        return new ProblemDetailsMapping(
            StatusCodes.Status400BadRequest,
            "https://httpstatuses.com/400",
            "Validation error",
            detail);
    }

    private static ProblemDetailsMapping Conflict(string detail)
    {
        return new ProblemDetailsMapping(
            StatusCodes.Status409Conflict,
            "https://httpstatuses.com/409",
            "Domain conflict",
            detail);
    }

    private static ProblemDetailsMapping UnprocessableEntity(string detail)
    {
        return new ProblemDetailsMapping(
            StatusCodes.Status422UnprocessableEntity,
            "https://httpstatuses.com/422",
            "Domain error",
            detail);
    }

    private static ProblemDetailsMapping InternalServerError()
    {
        return new ProblemDetailsMapping(
            StatusCodes.Status500InternalServerError,
            "https://httpstatuses.com/500",
            "Unexpected error",
            "An unexpected error occurred.");
    }
}
