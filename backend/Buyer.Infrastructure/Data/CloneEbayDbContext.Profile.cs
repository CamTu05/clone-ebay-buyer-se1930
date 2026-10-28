using Buyer.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Infrastructure.Data;

public partial class CloneEbayDbContext
{
    public virtual DbSet<UsernameChangeHistory> UsernameChangeHistories { get; set; } = null!;

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.lastUsernameChangedAt)
                .HasColumnType("datetime2(3)");
            entity.Property(x => x.normalizedUsername)
                .HasMaxLength(100)
                .IsRequired()
                .HasComputedColumnSql("UPPER(LTRIM(RTRIM([username])))", stored: true);
            entity.HasIndex(x => x.normalizedUsername)
                .IsUnique()
                .HasDatabaseName("UX_User_NormalizedUsername");
        });

        modelBuilder.Entity<UsernameChangeHistory>(entity =>
        {
            entity.ToTable("UsernameChangeHistory", "dbo");
            entity.HasKey(x => x.id);
            entity.Property(x => x.id).UseIdentityColumn();
            entity.Property(x => x.userId).IsRequired();
            entity.Property(x => x.oldUsername).HasMaxLength(100).IsRequired();
            entity.Property(x => x.newUsername).HasMaxLength(100).IsRequired();
            entity.Property(x => x.changedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(x => x.oldUsernameReservedUntilUtc)
                .HasColumnType("datetime2(3)");
            entity.Property(x => x.normalizedOldUsername)
                .HasMaxLength(100)
                .IsRequired()
                .HasComputedColumnSql("UPPER(LTRIM(RTRIM([oldUsername])))", stored: true);
            entity.HasIndex(x => new { x.normalizedOldUsername, x.oldUsernameReservedUntilUtc })
                .HasDatabaseName("IX_UsernameHistory_OldName_ReservedUntil");
            entity.HasIndex(x => new { x.userId, x.changedAtUtc })
                .HasDatabaseName("IX_UsernameHistory_User_ChangedAt");
            entity.HasOne<User>().WithMany()
                .HasForeignKey(x => x.userId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_UsernameChangeHistory_User");
        });
    }
}
