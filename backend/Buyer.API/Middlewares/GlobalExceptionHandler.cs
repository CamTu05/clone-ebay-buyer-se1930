using Buyer.Application.Catalog;
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
        if (exception is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException
            || exception.GetBaseException() is Microsoft.Data.SqlClient.SqlException { Number: 1205 or 2601 or 2627 }
            || exception is Microsoft.EntityFrameworkCore.DbUpdateException { InnerException: Microsoft.Data.SqlClient.SqlException { Number: 1205 or 2601 or 2627 } })
        {
            httpContext.Response.StatusCode = 409;
            await httpContext.Response.WriteAsJsonAsync(ApiEnvelope<object>.Failure("CONCURRENT_CHANGE", "Data changed concurrently. Refresh and retry."), cancellationToken);
            return true;
        }
        if (exception is Buyer.Application.BuyerRuleException rule)
        {
            httpContext.Response.StatusCode = rule.Status;
            await httpContext.Response.WriteAsJsonAsync(ApiEnvelope<object>.Failure(rule.Code, rule.Message), cancellationToken);
            return true;
        }
        if ((httpContext.Request.Path.StartsWithSegments("/api/products") || httpContext.Request.Path.StartsWithSegments("/api/listings"))
            && exception is CatalogUnavailableException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await httpContext.Response.WriteAsJsonAsync(ApiEnvelope<object>.Failure(
                "CATALOG_UNAVAILABLE",
                "The product catalog is temporarily unavailable."), cancellationToken);
            return true;
        }
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            ApiEnvelope<object>.Failure("INTERNAL_ERROR", "An unexpected error occurred"),
            cancellationToken);
        return true;
    }
}
