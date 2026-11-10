namespace Buyer.Application.Interfaces.Authentication;

public interface IClock
{
    DateTime UtcNow { get; }
}
