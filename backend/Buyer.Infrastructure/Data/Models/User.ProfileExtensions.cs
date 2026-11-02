namespace Buyer.Infrastructure.Data.Models;

public partial class User
{
    public DateTime? lastUsernameChangedAt { get; set; }
    public string normalizedUsername { get; private set; } = null!;
}
