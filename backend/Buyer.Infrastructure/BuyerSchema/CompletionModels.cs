using Microsoft.EntityFrameworkCore;
namespace Buyer.Infrastructure.BuyerSchema;
public sealed class OperationReceipt
{
    public string scope { get; set; } = "";
    public string operationKey { get; set; } = "";
    public string requestHash { get; set; } = "";
    public string responseJson { get; set; } = "";
    public DateTime createdAt { get; set; } = DateTime.UtcNow;
}
public sealed class EmailOutbox
{
    public long id { get; set; }
    public string recipient { get; set; } = "";
    public string subject { get; set; } = "";
    public string body { get; set; } = "";
    public DateTime createdAt { get; set; } = DateTime.UtcNow;
    public DateTime? sentAt { get; set; }
    public int attempts { get; set; }
}
public partial class BuyerSchemaContext
{
    public DbSet<OperationReceipt> OperationReceipts => Set<OperationReceipt>();
    public DbSet<EmailOutbox> EmailOutboxes => Set<EmailOutbox>();
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OperationReceipt>().ToTable("OperationReceipt").HasKey(x => new { x.scope, x.operationKey });
        modelBuilder.Entity<OperationReceipt>().Property(x => x.scope).HasMaxLength(256);
        modelBuilder.Entity<OperationReceipt>().Property(x => x.operationKey).HasMaxLength(100);
        modelBuilder.Entity<EmailOutbox>().ToTable("EmailOutbox");
        modelBuilder.Entity<Models.CheckoutGroup>().Property(x => x.taxAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Models.OrderTable>().Property(x => x.taxAmount).HasPrecision(18, 2);
    }
}

