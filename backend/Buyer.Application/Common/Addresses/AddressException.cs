namespace Buyer.Application.Common.Addresses;

public sealed class AddressException(string code, string message, AddressFailure failure,
    string? field = null) : Exception(message)
{
    public string Code { get; } = code;
    public AddressFailure Failure { get; } = failure;
    public string? Field { get; } = field;
}
