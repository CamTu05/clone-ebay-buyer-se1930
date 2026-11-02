namespace Buyer.API.Responses;

public sealed record ApiError(string Code, string? Field = null);
public sealed record ApiResponse<T>(bool Success, string Message, T? Data, IReadOnlyList<ApiError>? Errors)
{
    public static ApiResponse<T> Ok(T data) => new(true, "Success", data, null);
    public static ApiResponse<T> Fail(string message, string code, string? field = null)
        => new(false, message, default, [new ApiError(code, field)]);
}
