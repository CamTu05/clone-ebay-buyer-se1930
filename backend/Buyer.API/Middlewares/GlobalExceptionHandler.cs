using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Buyer.API.Responses;
using Buyer.Application.Catalog;

namespace Buyer.API.Middlewares;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred");
        if (httpContext.Request.Path.StartsWithSegments("/api/products"))
        {
            var unavailable = exception is CatalogUnavailableException;
            httpContext.Response.StatusCode = unavailable ? 503 : 500;
            await httpContext.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(
                unavailable ? "The product catalog is temporarily unavailable." : "An unexpected error occurred.",
                unavailable ? "CATALOG_UNAVAILABLE" : "INTERNAL_ERROR"), cancellationToken);
            return true;
        }
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred"
            }
        });
    }
}
