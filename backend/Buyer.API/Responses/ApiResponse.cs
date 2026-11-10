namespace Buyer.API.Controllers;

public sealed record ApiError(string Code, string? Field);

public sealed record ApiEnvelope<T>(bool Success, string Message, T? Data, IReadOnlyList<ApiError>? Errors)
{
    public static ApiEnvelope<T> Succeeded(T data, string message = "Success") =>
        new(true, message, data, null);

    public static ApiEnvelope<T> Failure(string code, string message = "Request failed", string? field = null) =>
        new(false, message, default, [new ApiError(code, field)]);
}
