using Buyer.Infrastructure.BuyerSchema;
using Buyer.Infrastructure.BuyerSchema.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Buyer.IntegrationTests;

public sealed class BuyerFeedbackRepositoryTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private DbContextOptions<BuyerSchemaContext> _options = null!;
    private BuyerSchemaContext Context() => new(_options);

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _connection.CreateFunction("sysutcdatetime", () => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture));
        _options = new DbContextOptionsBuilder<BuyerSchemaContext>().UseSqlite(_connection).Options;
        await using var db = Context();
        var schema = db.Database.GenerateCreateScript().Replace("N'", "'", StringComparison.Ordinal);
        await db.Database.ExecuteSqlRawAsync(schema);
        db.Users.AddRange(
            new User { id = 1, username = "buyer-a", email = "a@example.test", role = "BUYER", accountStatus = "ACTIVE", emailVerified = true },
            new User { id = 2, username = "buyer-b", email = "b@example.test", role = "BUYER", accountStatus = "ACTIVE", emailVerified = true },
            new User { id = 10, username = "camera_store", email = "seller@example.test", role = "SELLER", accountStatus = "ACTIVE", avatarURL = "https://example.com/seller.png" });
        db.Categories.Add(new Category { id = 1, name = "Cameras", slug = "cameras", isActive = true });
        db.CheckoutGroups.AddRange(Group(1, 1), Group(2, 1), Group(3, 1), Group(4, 2));
        db.OrderTables.AddRange(Order(1, 1, 1), Order(2, 2, 1), Order(3, 3, 1), Order(4, 4, 2));
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO Listing(id,productId,sellerId,categoryId,shippingPolicyId,title,description,itemCondition,listingType,price,quantity,status,createdAt,publishedAt,endedAt,rowVersion)
            VALUES(1,NULL,10,1,NULL,'Standalone camera',NULL,'New','FIXED_PRICE',500000,1,'ENDED',CURRENT_TIMESTAMP,NULL,NULL,X'01'),
                  (2,NULL,10,1,NULL,'Camera strap',NULL,'New','FIXED_PRICE',100000,1,'ENDED',CURRENT_TIMESTAMP,NULL,NULL,X'02');
            """);
        db.OrderItems.AddRange(
            Item(1, 1, 1, "Standalone camera", "https://example.com/camera.jpg", 500000),
            Item(2, 2, 1, "Standalone camera", null, 510000),
            Item(3, 3, 1, "Standalone camera", "https://example.com/camera.jpg", 520000),
            Item(4, 3, 2, "Camera strap", null, 100000),
            Item(5, 4, 1, "Standalone camera", null, 530000));
        var tied = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
        db.BuyerFeedbacks.AddRange(
            new BuyerFeedback { id = 1, orderId = 1, buyerId = 1, sellerId = 10, comment = "Old", createdAt = tied.AddDays(-1) },
            new BuyerFeedback { id = 2, orderId = 2, buyerId = 1, sellerId = 10, comment = "Tie lower", createdAt = tied },
            new BuyerFeedback { id = 3, orderId = 3, buyerId = 1, sellerId = 10, comment = "Tie higher", createdAt = tied },
            new BuyerFeedback { id = 4, orderId = 4, buyerId = 2, sellerId = 10, comment = "Other buyer", createdAt = tied.AddDays(1) });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => _connection.DisposeAsync().AsTask();

    [Fact]
    public async Task QueryIsOwnedStablePagedAndProjectsOrderSnapshots()
    {
        await using var db = Context();
        var repository = new ProfileRepository(db);

        var first = await repository.GetFeedbackAsync(1, 1, 2);
        var second = await repository.GetFeedbackAsync(1, 2, 2);
        var empty = await repository.GetFeedbackAsync(3, 1, 10);

        Assert.Equal(3, first.TotalItems);
        Assert.Equal([3L, 2L], first.Items.Select(item => item.Id));
        Assert.Equal(1L, Assert.Single(second.Items).Id);
        Assert.All(first.Items, item => Assert.Equal("camera_store", item.Seller.Username));
        Assert.All(first.Items, item => Assert.True(item.VerifiedPurchase));
        Assert.All(first.Items, item => Assert.Equal("VND", item.Currency));
        var newest = first.Items[0];
        Assert.Equal(3, newest.OrderId);
        Assert.Equal([3L, 4L], newest.PurchaseItems.Select(item => item.OrderItemId));
        Assert.Equal("Standalone camera", newest.PurchaseItems[0].Title);
        Assert.Null(newest.PurchaseItems[1].ImageUrl);
        Assert.Equal(520000, newest.PurchaseItems[0].UnitPrice);
        Assert.Equal(0, empty.TotalItems);
        Assert.Empty(empty.Items);
        Assert.DoesNotContain(first.Items, item => item.Comment == "Other buyer");
    }

    private static OrderItem Item(long id, long orderId, int listingId, string title, string? image, decimal price) => new()
    {
        id = id, orderId = orderId, listingId = listingId, productId = null,
        quantity = 1, unitPrice = price, productTitle = title, imageUrl = image
    };

    private static CheckoutGroup Group(long id, int buyerId) => new()
    {
        id = id, buyerId = buyerId, subtotal = 100, discountAmount = 0,
        shippingFee = 0, taxAmount = 0, totalAmount = 100,
        currency = "VND", status = "CONFIRMED"
    };

    private static OrderTable Order(long id, long groupId, int buyerId) => new()
    {
        id = id, checkoutGroupId = groupId, buyerId = buyerId, sellerId = 10,
        shippingFullName = "Buyer", shippingEmail = "buyer@example.test",
        shippingPhone = "000", shippingStreet = "Street", shippingCity = "City",
        shippingCountry = "Vietnam", subtotal = 100, discountAmount = 0,
        shippingFee = 0, taxAmount = 0, totalAmount = 100,
        currency = "VND", status = "COMPLETED"
    };
}
