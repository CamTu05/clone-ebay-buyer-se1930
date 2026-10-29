using System.Data;
using System.Data.Common;
using Buyer.Infrastructure.Catalog;
using Buyer.Infrastructure.Data;
using Buyer.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Buyer.UnitTests;

public sealed class ProductCatalogRepositoryTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CatalogIncludesOnlyAvailableListingsAtTheRequestedTime()
    {
        await using var database = CreateDatabase();
        database.Products.AddRange(
            ProductWithListing(1),
            ProductWithListing(2, listing => listing.price = 0),
            ProductWithListing(3, MakeAuction),
            ProductWithListing(4, listing => listing.status = "DRAFT"),
            ProductWithListing(5, listing => listing.quantity = 0),
            ProductWithListing(6, listing => listing.price = null),
            ProductWithListing(7, listing => listing.publishedAt = Now.AddTicks(1)),
            ProductWithListing(8, listing => listing.endedAt = Now),
            ProductWithListing(9, listing =>
            {
                MakeAuction(listing);
                listing.Auction!.status = "SCHEDULED";
            }),
            ProductWithListing(10, listing =>
            {
                MakeAuction(listing);
                listing.Auction!.startTime = Now.AddTicks(1);
            }),
            ProductWithListing(11, listing =>
            {
                MakeAuction(listing);
                listing.Auction!.endTime = Now;
            }),
            ProductWithListing(12, listing => listing.listingType = "AUCTION"),
            ProductWithListing(13, listing =>
            {
                listing.publishedAt = Now;
                listing.endedAt = Now.AddTicks(1);
            }),
            ProductWithListing(14, listing => listing.status = "PAUSED"));
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        var result = await new ProductCatalogRepository(database)
            .GetAvailableProductsAsync(1, 20, Now, default);

        Assert.Equal(4, result.TotalItems);
        Assert.Equal(new[] { 1, 2, 3, 13 }, result.Items.Select(item => item.ProductId));
        Assert.Equal(0, result.Items.Single(item => item.ProductId == 2).Price);
        Assert.Equal(80, result.Items.Single(item => item.ProductId == 3).Price);
        Assert.Empty(database.ChangeTracker.Entries());
    }

    [Fact]
    public async Task CatalogCountsProductsOnceAndSelectsTheCheapestEligibleListingWithStableTies()
    {
        await using var database = CreateDatabase();
        var auctionProduct = ProductWithListing(10);
        var auction = NewListing(101, 10);
        MakeAuction(auction);
        auctionProduct.Listings.Add(auction);

        var fixedProduct = ProductWithListing(20);
        fixedProduct.Listings.Add(NewListing(201, 20, 50));
        fixedProduct.Listings.Add(NewListing(200, 20, 50));
        var unavailableCheaperListing = NewListing(199, 20, 1);
        unavailableCheaperListing.status = "SOLD";
        fixedProduct.Listings.Add(unavailableCheaperListing);
        database.Products.AddRange(fixedProduct, auctionProduct);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        var result = await new ProductCatalogRepository(database)
            .GetAvailableProductsAsync(1, 20, Now, default);

        Assert.Equal(2, result.TotalItems);
        Assert.Collection(result.Items,
            item =>
            {
                Assert.Equal(10, item.ProductId);
                Assert.Equal(101, item.ListingId);
                Assert.Equal(80, item.Price);
                Assert.Equal("AUCTION", item.ListingType);
            },
            item =>
            {
                Assert.Equal(20, item.ProductId);
                Assert.Equal(200, item.ListingId);
                Assert.Equal(50, item.Price);
                Assert.Equal("FIXED_PRICE", item.ListingType);
            });
    }

    [Fact]
    public async Task CatalogUsesPrimaryImageThenSortOrderThenImageIdAndAllowsMissingImages()
    {
        await using var database = CreateDatabase();
        var primaryProduct = ProductWithListing(1);
        primaryProduct.ProductImages.Add(NewImage(1, 1, false, 0));
        primaryProduct.ProductImages.Add(NewImage(10, 1, true, 10));
        primaryProduct.ProductImages.Add(NewImage(11, 1, true, 2));
        primaryProduct.ProductImages.Add(NewImage(9, 1, true, 2));
        var fallbackProduct = ProductWithListing(2);
        fallbackProduct.ProductImages.Add(NewImage(4, 2, false, 1));
        fallbackProduct.ProductImages.Add(NewImage(3, 2, false, 1));
        database.Products.AddRange(primaryProduct, fallbackProduct, ProductWithListing(3));
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        var result = await new ProductCatalogRepository(database)
            .GetAvailableProductsAsync(1, 20, Now, default);

        Assert.Equal("https://example.test/9.png", result.Items[0].ImageUrl);
        Assert.Equal("https://example.test/3.png", result.Items[1].ImageUrl);
        Assert.Null(result.Items[2].ImageUrl);
    }

    [Fact]
    public async Task PaginationKeepsProductOrderAndTotalForLastMissingAndVeryLargePages()
    {
        await using var database = CreateDatabase();
        database.Products.AddRange(new[] { 9, 1, 7, 3, 5 }.Select(id => ProductWithListing(id)));
        database.Products.Add(ProductWithListing(2, listing => listing.status = "ENDED"));
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        var repository = new ProductCatalogRepository(database);

        var middlePage = await repository.GetAvailableProductsAsync(2, 2, Now, default);
        var lastPage = await repository.GetAvailableProductsAsync(3, 2, Now, default);
        var missingPage = await repository.GetAvailableProductsAsync(4, 2, Now, default);
        var largePage = await repository.GetAvailableProductsAsync(int.MaxValue, 100, Now, default);

        Assert.Equal(new[] { 5, 7 }, middlePage.Items.Select(item => item.ProductId));
        Assert.Equal(9, Assert.Single(lastPage.Items).ProductId);
        Assert.Empty(missingPage.Items);
        Assert.Empty(largePage.Items);
        Assert.All(new[] { middlePage, lastPage, missingPage, largePage }, page => Assert.Equal(5, page.TotalItems));
    }

    [Fact]
    public async Task EmptyCatalogReturnsZeroTotalAndNoItems()
    {
        await using var database = CreateDatabase();

        var result = await new ProductCatalogRepository(database)
            .GetAvailableProductsAsync(1, 20, Now, default);

        Assert.Equal(0, result.TotalItems);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task CatalogProjectionTranslatesWithTheSqlServerProvider()
    {
        var interceptor = new TranslationInterceptor();
        var options = new DbContextOptionsBuilder<CloneEbayDbContext>()
            .UseSqlServer("Server=unused.invalid;Database=TranslationOnly;Integrated Security=true;TrustServerCertificate=true")
            .AddInterceptors(interceptor)
            .Options;
        await using var database = new CloneEbayDbContext(options);

        await Assert.ThrowsAsync<TranslationReachedException>(() =>
            new ProductCatalogRepository(database).GetAvailableProductsAsync(1, 20, Now, default));

        Assert.Equal(2, interceptor.Commands.Count);
        Assert.Contains("COUNT(*)", interceptor.Commands[0]);
        Assert.Contains("ProductImage", interceptor.Commands[1]);
        Assert.Contains("Listing", interceptor.Commands[1]);
        Assert.Contains("ORDER BY", interceptor.Commands[1]);
        Assert.Contains("OFFSET", interceptor.Commands[1]);
    }

    private static CloneEbayDbContext CreateDatabase() => new(
        new DbContextOptionsBuilder<CloneEbayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Product ProductWithListing(int id, Action<Listing>? configure = null)
    {
        var product = new Product { id = id, title = $"Product {id}", categoryId = 1 };
        var listing = NewListing(id, id);
        configure?.Invoke(listing);
        product.Listings.Add(listing);
        return product;
    }

    private static Listing NewListing(int id, int productId, decimal price = 100) => new()
    {
        id = id,
        productId = productId,
        sellerId = 1,
        listingType = "FIXED_PRICE",
        status = "ACTIVE",
        price = price,
        quantity = 1
    };

    private static void MakeAuction(Listing listing)
    {
        listing.listingType = "AUCTION";
        listing.price = null;
        listing.Auction = new Auction
        {
            id = listing.id,
            listingId = listing.id,
            startingPrice = 50,
            currentPrice = 80,
            bidIncrement = 1,
            status = "ACTIVE",
            startTime = Now,
            endTime = Now.AddHours(1),
            rowVersion = []
        };
    }

    private static ProductImage NewImage(int id, int productId, bool primary, int order) => new()
    {
        id = id,
        productId = productId,
        isPrimary = primary,
        sortOrder = order,
        imageUrl = $"https://example.test/{id}.png"
    };

    // Suppress connection opening and provide only COUNT's result; no SQL Server is contacted.
    // The second command proves the real SQL Server provider translated the complete projection.
    private sealed class TranslationInterceptor : DbConnectionInterceptor, IDbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());

        public ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            if (Commands.Count > 1)
            {
                throw new TranslationReachedException();
            }

            var table = new DataTable();
            table.Columns.Add("Count", typeof(int));
            table.Rows.Add(1);
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }

    private sealed class TranslationReachedException : Exception;
}
