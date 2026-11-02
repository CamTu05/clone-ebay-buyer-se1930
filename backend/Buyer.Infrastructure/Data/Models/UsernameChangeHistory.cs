namespace Buyer.Infrastructure.Data.Models;

public sealed class UsernameChangeHistory
{
    public long id { get; set; }
    public int userId { get; set; }
    public string oldUsername { get; set; } = null!;
    public string newUsername { get; set; } = null!;
    public DateTime changedAtUtc { get; set; }
    public DateTime oldUsernameReservedUntilUtc { get; set; }
    public string normalizedOldUsername { get; private set; } = null!;
}
