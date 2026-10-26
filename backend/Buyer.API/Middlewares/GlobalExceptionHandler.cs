using Buyer.API.Controllers;
using Microsoft.AspNetCore.Diagnostics;

namespace Buyer.API.Middlewares;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred");
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            ApiEnvelope<object>.Failure("INTERNAL_ERROR", "An unexpected error occurred"),
            cancellationToken);
        return true;
    }
}
