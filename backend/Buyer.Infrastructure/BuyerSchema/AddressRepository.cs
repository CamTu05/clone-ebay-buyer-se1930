using System.Data;
using Buyer.Application.Interfaces.Addresses;
using Buyer.Application.Interfaces.Authentication;
using Buyer.Domain.Addresses;
using Buyer.Infrastructure.BuyerSchema.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Infrastructure.BuyerSchema;

public sealed class AddressRepository(BuyerSchemaContext db, IClock clock) : IAddressRepository
{
    public async Task<IReadOnlyList<ShippingAddress>> ListAsync(int userId, CancellationToken ct)
    {
        await CheckAccountAsync(userId, false, ct);
        var rows = await db.Addresses.AsNoTracking().Where(a => a.userId == userId)
            .OrderBy(a => a.id).ToListAsync(ct);
        return rows.Select(ToAddress).ToArray();
    }

    public async Task<T> MutateAsync<T>(int userId, Func<IList<ShippingAddress>, T> change, CancellationToken ct)
    {
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            // Serialize every mutation on the parent user, including an empty address book.
            // This prevents two first-address inserts or two default changes racing each other.
            await CheckAccountAsync(userId, true, ct);
            var rows = await db.Addresses.Where(a => a.userId == userId).OrderBy(a => a.id).ToListAsync(ct);
            var book = rows.Select(ToAddress).ToList();
            var result = change(book);
            var now = clock.UtcNow;

            // Flush demotions first: SQL Server's filtered unique index is checked per statement.
            foreach (var row in rows.Where(r => r.isDefault && !book.Any(a => a.Id == r.id && a.IsDefault)))
            {
                row.isDefault = false;
                row.updatedAt = now;
            }
            await db.SaveChangesAsync(ct);

            var removed = rows.Where(r => !book.Any(a => a.Id == r.id)).ToArray();
            var removedIds = removed.Select(r => r.id).ToArray();
            if (removedIds.Length > 0)
            {
                // Orders own immutable shipping snapshots. Clear only the optional address-book
                // pointer so deletion cannot break the FK or change a historical destination.
                await db.OrderTables.Where(o => o.addressId != null && removedIds.Contains(o.addressId.Value))
                    .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.addressId, (int?)null), ct);
                db.Addresses.RemoveRange(removed);
            }

            var inserts = new List<(ShippingAddress Address, Address Row)>();
            foreach (var address in book)
            {
                var row = rows.FirstOrDefault(r => r.id == address.Id);
                if (row is null)
                {
                    row = new Address { userId = userId, createdAt = now };
                    db.Addresses.Add(row);
                    inserts.Add((address, row));
                }
                row.fullName = address.FullName;
                row.phone = address.Phone;
                row.street = address.Street;
                row.addressLine2 = address.AddressLine2;
                row.city = address.City;
                row.state = address.State;
                row.postalCode = address.PostalCode;
                row.country = address.Country;
                row.countryCode = address.CountryCode;
                row.isDefault = address.IsDefault;
                if (db.Entry(row).State == EntityState.Added || db.Entry(row).Properties.Any(p => p.IsModified))
                    row.updatedAt = now;
            }
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            foreach (var insert in inserts) insert.Address.Id = insert.Row.id;
            return result;
        }
        catch (Exception exception) when (exception is SqlException { Number: 1205 or 2601 or 2627 }
            || exception is DbUpdateException { InnerException: SqlException { Number: 1205 or 2601 or 2627 } })
        {
            throw new AddressException("ADDRESS_CONFLICT", "Addresses changed concurrently. Reload and try again.", AddressFailure.Conflict);
        }
    }

    private async Task CheckAccountAsync(int userId, bool lockUser, CancellationToken ct)
    {
        var query = lockUser && db.Database.IsSqlServer()
            ? db.Users.FromSqlInterpolated($"SELECT * FROM dbo.[User] WITH (UPDLOCK, HOLDLOCK) WHERE id = {userId}")
            : db.Users.Where(u => u.id == userId);
        var active = await query.AnyAsync(u => u.accountStatus == "ACTIVE", ct);
        if (!active)
            throw new AddressException("AUTH_ACCOUNT_UNAVAILABLE", "Account is unavailable.", AddressFailure.Forbidden);
    }

    private static ShippingAddress ToAddress(Address a) => new()
    {
        Id = a.id,
        FullName = a.fullName,
        Phone = a.phone,
        Street = a.street,
        AddressLine2 = a.addressLine2,
        City = a.city,
        State = a.state,
        PostalCode = a.postalCode,
        Country = a.country,
        CountryCode = a.countryCode,
        IsDefault = a.isDefault
    };
}

