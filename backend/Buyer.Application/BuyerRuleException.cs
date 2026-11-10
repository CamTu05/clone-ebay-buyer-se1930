namespace Buyer.Application;
public sealed class BuyerRuleException(string code, string message, int status = 400) : Exception(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
}
