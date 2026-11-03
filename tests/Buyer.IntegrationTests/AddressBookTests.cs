using Buyer.Application.Addresses;
using Buyer.Application.Authentication;
using Buyer.Infrastructure.Data;
using Buyer.Infrastructure.Data.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Buyer.IntegrationTests;

// A disposable relational database verifies writes, unique defaults, FK handling and rollback.
// SQL Server's UPDLOCK serialization additionally needs the optional SQL Server test described in docs.
public sealed class AddressBookTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private DbContextOptions<CloneEbayDbContext> _options = null!;
    private sealed class Clock : IClock { public DateTime UtcNow => new(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc); }
    private CloneEbayDbContext Context() => new(_options);
    private static AddressService Service(CloneEbayDbContext db) => new(new AddressRepository(db, new Clock()));
    private static CreateAddressRequest Input(string name = "Buyer", bool primary = false)
        => new(name, "+84 901234567", "123 Main street", "Hanoi", "vn", PostalCode: "10000", IsDefault: primary);

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _connection.CreateFunction("sysutcdatetime", () => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture));
        _options = new DbContextOptionsBuilder<CloneEbayDbContext>().UseSqlite(_connection).Options;
        await using var db = Context();
        await db.Database.EnsureCreatedAsync();
        db.Users.AddRange(new User { id = 1, username = "buyer", email = "buyer@example.test", role = "BUYER", accountStatus = "ACTIVE" },
            new User { id = 2, username = "other", email = "other@example.test", role = "BUYER", accountStatus = "ACTIVE" });
        await db.SaveChangesAsync();
    }
    public Task DisposeAsync() => _connection.DisposeAsync().AsTask();

    private async Task<AddressResponse> Create(CreateAddressRequest? input = null, int user = 1)
    {
        await using var db = Context();
        return await Service(db).CreateAsync(user, input ?? Input(), default);
    }

    [Fact]
    public async Task FirstAddressIsPrimaryAndMoreAddressesAreAllowed()
    {
        var first = await Create();
        var second = await Create(Input("Office"));
        Assert.True(first.Id > 0);
        Assert.True(first.IsDefault);
        Assert.False(second.IsDefault);
        Assert.Equal("VN", first.CountryCode);
        await using var db = Context();
        var list = await Service(db).ListAsync(1, default);
        Assert.Equal(2, list.Count);
        Assert.Single(list.Where(a => a.IsDefault));
    }

    [Fact]
    public async Task SwitchingDefaultDemotesOldDefaultBeforeUniqueIndexCheck()
    {
        var first = await Create();
        var second = await Create(Input("Office"));
        await using (var db = Context()) await Service(db).SetDefaultAsync(1, second.Id, default);
        await using (var db = Context()) await Service(db).SetDefaultAsync(1, second.Id, default);
        await using var verify = Context();
        var list = await Service(verify).ListAsync(1, default);
        Assert.Equal(second.Id, Assert.Single(list.Where(a => a.IsDefault)).Id);
        Assert.False(list.Single(a => a.Id == first.Id).IsDefault);
    }

    [Fact]
    public async Task CreatingPrimaryAndEditingPrimaryPreserveSingleDefault()
    {
        await Create();
        var second = await Create(Input("Office", true));
        await using (var db = Context())
        {
            var edited = await Service(db).UpdateAsync(1, second.Id,
                new UpdateAddressRequest(Street: "  456 New street  ", AddressLine2: "Unit 2"), default);
            Assert.Equal("456 New street", edited.Street);
            Assert.Equal("Office", edited.FullName);
            Assert.True(edited.IsDefault);
        }
        await using var verify = Context();
        Assert.Single((await Service(verify).ListAsync(1, default)).Where(a => a.IsDefault));
    }

    [Fact]
    public async Task DeletingDefaultPromotesOldestRemainingAndAllowsEmptyBook()
    {
        var first = await Create();
        var second = await Create(Input("Office"));
        await using (var db = Context()) await Service(db).DeleteAsync(1, first.Id, default);
        await using (var db = Context()) Assert.True((await Service(db).GetAsync(1, second.Id, default)).IsDefault);
        await using (var db = Context()) await Service(db).DeleteAsync(1, second.Id, default);
        await using var verify = Context();
        Assert.Empty(await Service(verify).ListAsync(1, default));
    }

    [Fact]
    public async Task ForeignAddressesCannotBeReadUpdatedDeletedMadeDefaultOrSelected()
    {
        var foreign = await Create(user: 2);
        Func<AddressService, Task>[] attempts = [
            s => s.GetAsync(1, foreign.Id, default),
            s => s.UpdateAsync(1, foreign.Id, new UpdateAddressRequest(City: "Other city"), default),
            s => s.DeleteAsync(1, foreign.Id, default),
            s => s.SetDefaultAsync(1, foreign.Id, default),
            s => s.SelectForCheckoutAsync(1, foreign.Id, default)];
        foreach (var attempt in attempts)
        {
            await using var db = Context();
            var error = await Assert.ThrowsAsync<AddressException>(() => attempt(Service(db)));
            Assert.Equal("ADDRESS_NOT_FOUND", error.Code);
        }
    }

    [Fact]
    public async Task InvalidEditRollsBackAndDefaultCannotBeUnsetWithoutReplacement()
    {
        var first = await Create();
        await using (var db = Context())
            await Assert.ThrowsAsync<AddressException>(() => Service(db).UpdateAsync(1, first.Id, new UpdateAddressRequest(FullName: "   "), default));
        await using (var db = Context())
            await Assert.ThrowsAsync<AddressException>(() => Service(db).UpdateAsync(1, first.Id, new UpdateAddressRequest(IsDefault: false), default));
        await using var verify = Context();
        var unchanged = await Service(verify).GetAsync(1, first.Id, default);
        Assert.Equal("Buyer", unchanged.FullName);
        Assert.True(unchanged.IsDefault);
    }

    [Fact]
    public async Task OptionalFieldCanBeClearedAndCheckoutSelectionDoesNotChangeDefault()
    {
        var first = await Create();
        var second = await Create(Input("Office") with { AddressLine2 = "Unit 1" });
        await using (var db = Context())
        {
            var updated = await Service(db).UpdateAsync(1, second.Id, new UpdateAddressRequest(AddressLine2: ""), default);
            Assert.Null(updated.AddressLine2);
            var selected = await Service(db).SelectForCheckoutAsync(1, second.Id, default);
            Assert.Equal(second.Id, selected.Id);
            Assert.False(selected.IsDefault);
        }
        await using var verify = Context();
        Assert.True((await Service(verify).GetAsync(1, first.Id, default)).IsDefault);
    }

    [Fact]
    public async Task EditsAndDeletionKeepHistoricalOrderSnapshot()
    {
        var first = await Create();
        await using (var db = Context())
        {
            db.OrderTables.Add(new OrderTable
            {
                buyerId = 1, sellerId = 2, addressId = first.Id, shippingFullName = "Original name",
                shippingPhone = "0901234567", shippingStreet = "Original street", shippingCity = "Original city",
                shippingCountry = "Vietnam", currency = "VND", status = "PENDING_PAYMENT"
            });
            await db.SaveChangesAsync();
        }
        await using (var db = Context()) await Service(db).UpdateAsync(1, first.Id, new UpdateAddressRequest(Street: "New street"), default);
        await using (var db = Context()) await Service(db).DeleteAsync(1, first.Id, default);
        await using var verify = Context();
        var order = await verify.OrderTables.SingleAsync();
        Assert.Null(order.addressId);
        Assert.Equal("Original street", order.shippingStreet);
        Assert.Equal("Original name", order.shippingFullName);
    }

    [Fact]
    public async Task SuspendedAccountsCannotUsePreviouslyIssuedIdentity()
    {
        await using (var db = Context())
        {
            (await db.Users.SingleAsync(u => u.id == 1)).accountStatus = "SUSPENDED";
            await db.SaveChangesAsync();
        }
        await using var verify = Context();
        var error = await Assert.ThrowsAsync<AddressException>(() => Service(verify).ListAsync(1, default));
        Assert.Equal(AddressFailure.Forbidden, error.Failure);
    }

    [Theory]
    [InlineData("", "0901234567", "VN")]
    [InlineData("Buyer", "phone", "VN")]
    [InlineData("Buyer", "0901234567", "XX")]
    public async Task InvalidInputsDoNotInsert(string name, string phone, string country)
    {
        await Assert.ThrowsAsync<AddressException>(() => Create(Input(name) with { Phone = phone, CountryCode = country }));
        await using var verify = Context();
        Assert.Empty(await verify.Addresses.ToListAsync());
    }
}
