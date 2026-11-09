using Buyer.Application.Interfaces.ListingCatalog;
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
        if (httpContext.Request.Path.StartsWithSegments("/api/listings")
            && exception is CatalogUnavailableException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await httpContext.Response.WriteAsJsonAsync(ApiEnvelope<object>.Failure(
                "CATALOG_UNAVAILABLE",
                "The listing catalog is temporarily unavailable."), cancellationToken);
            return true;
        }
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            ApiEnvelope<object>.Failure("INTERNAL_ERROR", "An unexpected error occurred"),
            cancellationToken);
        return true;
    }
}
