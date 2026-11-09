using System.Data;
using System.Data.Common;
using Buyer.Application.Interfaces.ListingCatalog;
using Buyer.Infrastructure.Repositories.ListingCatalog;
using Buyer.Infrastructure.Repositories.Addresses;
using Buyer.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Buyer.UnitTests;

public sealed class ListingCatalogRepositoryTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task OneProductWithTwoActiveListingsReturnsTwoItemsAndCountsListings()
    {
        await using var database = CreateDatabase();
        var product = NewProduct(1);
        product.Listings.Add(NewListing(11, 1, 800));
        var auction = NewListing(12, 1);
        MakeAuction(auction, 500);
        product.Listings.Add(auction);
        database.Products.Add(product);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        var result = await Repository(database).GetAvailableListingsAsync(1, 20, Now, default);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(new[] { 12, 11 }, result.Items.Select(item => item.ListingId));
        Assert.All(result.Items, item => Assert.Equal(1, item.ProductId));
        Assert.Equal(800, result.Items.Single(item => item.ListingId == 11).EffectivePrice);
        var auctionItem = result.Items.Single(item => item.ListingId == 12);
        Assert.Equal(500, auctionItem.EffectivePrice);
        Assert.Equal(500, auctionItem.CurrentBid);
        Assert.Empty(database.ChangeTracker.Entries());
    }

    [Fact]
    public async Task AvailabilityRulesExcludeInvalidListingsAndAuctions()
    {
        await using var database = CreateDatabase();
        database.Products.AddRange(
            ProductWithListing(1),
            ProductWithListing(2, listing => listing.status = "DRAFT"),
            ProductWithListing(3, listing => listing.quantity = 0),
            ProductWithListing(4, listing => listing.publishedAt = Now.AddTicks(1)),
            ProductWithListing(5, listing => listing.endedAt = Now),
            ProductWithListing(6, listing => listing.price = null),
            ProductWithListing(7, listing => listing.listingType = "AUCTION"),
            ProductWithListing(8, listing => { MakeAuction(listing); listing.Auction!.status = "ENDED"; }),
            ProductWithListing(9, listing => { MakeAuction(listing); listing.Auction!.startTime = Now.AddTicks(1); }),
            ProductWithListing(10, listing => { MakeAuction(listing); listing.Auction!.endTime = Now; }),
            ProductWithListing(11, listing => { listing.publishedAt = Now; listing.endedAt = Now.AddTicks(1); }),
            ProductWithListing(12, listing => MakeAuction(listing, 75)));
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        var result = await Repository(database).GetAvailableListingsAsync(1, 20, Now, default);

        Assert.Equal(new[] { 11, 12, 1 }, result.Items.Select(item => item.ListingId));
        Assert.Equal(75, result.Items[1].EffectivePrice);
    }

    [Fact]
    public async Task ProjectionUsesPrimaryImageAndReturnsListingCardFields()
    {
        await using var database = CreateDatabase();
        var product = NewProduct(1);
        product.brand = "Brand";
        product.itemCondition = "NEW";
        product.ProductImages.Add(NewImage(1, 1, false, 0));
        product.ProductImages.Add(NewImage(10, 1, true, 10));
        product.ProductImages.Add(NewImage(9, 1, true, 2));
        var listing = NewListing(1, 1);
        listing.allowBestOffer = true;
        listing.publishedAt = Now;
        listing.shippingPolicy = new ShippingPolicy
        {
            id = 1,
            sellerId = 1,
            name = "Free",
            shippingType = "FREE",
            isActive = true
        };
        product.Listings.Add(listing);
        database.Products.Add(product);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        var item = Assert.Single((await Repository(database).GetAvailableListingsAsync(1, 20, Now, default)).Items);

        Assert.Equal("Product 1", item.Title);
        Assert.Equal("https://example.test/9.png", item.PrimaryImageUrl);
        Assert.Equal("Brand", item.Brand);
        Assert.Equal("NEW", item.ItemCondition);
        Assert.True(item.AllowBestOffer);
        Assert.True(item.FreeShipping);
        Assert.Equal(Now, item.PublishedAt);
    }

    [Fact]
    public async Task PaginationUsesListingCountAndStablePublishedAtThenIdOrder()
    {
        await using var database = CreateDatabase();
        var product = NewProduct(1);
        foreach (var id in new[] { 1, 5, 3, 2, 4 })
        {
            var listing = NewListing(id, 1);
            listing.publishedAt = Now;
            product.Listings.Add(listing);
        }
        database.Products.Add(product);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        var repository = Repository(database);

        var page = await repository.GetAvailableListingsAsync(2, 2, Now, default);
        var missing = await repository.GetAvailableListingsAsync(4, 2, Now, default);

        Assert.Equal(new[] { 3, 2 }, page.Items.Select(item => item.ListingId));
        Assert.Equal(5, page.TotalItems);
        Assert.Empty(missing.Items);
        Assert.Equal(5, missing.TotalItems);
    }

    [Fact]
    public async Task DetailUsesTheRequestedListingSellerPriceImagesAndShipping()
    {
        await using var database = CreateDatabase();
        var category = new Category { id = 1, name = "Electronics", slug = "electronics", isActive = true };
        var sellerOne = NewSeller(1, "seller-one");
        var sellerTwo = NewSeller(2, "seller-two");
        var product = NewProduct(1);
        product.category = category;
        product.ProductImages.Add(NewImage(1, 1, false, 0));
        product.ProductImages.Add(NewImage(3, 1, true, 2));
        product.ProductImages.Add(NewImage(2, 1, true, 2));
        var first = NewListing(11, 1, 800);
        first.seller = sellerOne;
        var second = NewListing(12, 1, 700);
        second.sellerId = 2;
        second.seller = sellerTwo;
        second.shippingPolicy = new ShippingPolicy
        {
            id = 1,
            sellerId = 2,
            seller = sellerTwo,
            name = "Free",
            shippingType = "FREE",
            isActive = true,
            ShippingOptions = [new ShippingOption
            {
                id = 1, shippingPolicyId = 1, serviceName = "Standard", serviceType = "STANDARD",
                flatRate = null, estimatedMinDays = 2, estimatedMaxDays = 4, isActive = true
            }]
        };
        product.Listings.Add(first);
        product.Listings.Add(second);
        database.AddRange(category, sellerOne, sellerTwo, product);
        database.Entry(sellerOne).Property("normalizedUsername").CurrentValue = "SELLER-ONE";
        database.Entry(sellerTwo).Property("normalizedUsername").CurrentValue = "SELLER-TWO";
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        var detail = await Repository(database).GetAvailableListingAsync(12, Now, default);

        Assert.NotNull(detail);
        Assert.Equal(12, detail.ListingId);
        Assert.Equal("seller-two", detail.SellerUsername);
        Assert.Equal(700, detail.EffectivePrice);
        Assert.Equal(new[] { 2, 3, 1 }, detail.Images.Select(image => image.ImageId));
        Assert.Equal("FREE", detail.Shipping.ShippingType);
        Assert.Equal(0, Assert.Single(detail.Shipping.AvailableOptions).FlatRate);
        Assert.Null(detail.Auction);
    }

    [Fact]
    public async Task AuctionDetailUsesCurrentPriceAndDoesNotFallbackToProductIdentity()
    {
        await using var database = CreateDatabase();
        var category = new Category { id = 1, name = "Electronics", slug = "electronics", isActive = true };
        var seller = NewSeller(1, "auction-seller");
        var product = NewProduct(1);
        product.category = category;
        var auctionListing = NewListing(21, 1);
        auctionListing.seller = seller;
        MakeAuction(auctionListing, 450);
        product.Listings.Add(auctionListing);
        database.AddRange(category, seller, product);
        database.Entry(seller).Property("normalizedUsername").CurrentValue = "AUCTION-SELLER";
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        var detail = await Repository(database).GetAvailableListingAsync(21, Now, default);
        var missing = await Repository(database).GetAvailableListingAsync(1, Now, default);

        Assert.NotNull(detail);
        Assert.Equal(450, detail.EffectivePrice);
        Assert.Equal(450, detail.Auction!.CurrentPrice);
        Assert.Null(detail.BuyItNowPrice);
        Assert.Null(missing);
    }

    [Fact]
    public async Task SearchFiltersEffectivePriceAndSortComposeOnTheSameListingRows()
    {
        await using var database = CreateDatabase();
        var product = NewProduct(1);
        product.title = "iPhone 15";
        product.description = "Apple smartphone";
        product.brand = "Apple";
        product.itemCondition = "NEW";
        var fixedListing = NewListing(11, 1, 800);
        fixedListing.sellerId = 2;
        fixedListing.allowBestOffer = true;
        var auctionListing = NewListing(12, 1);
        auctionListing.sellerId = 2;
        auctionListing.allowBestOffer = true;
        MakeAuction(auctionListing, 500);
        auctionListing.Auction!.bidCount = 5;
        product.Listings.Add(fixedListing);
        product.Listings.Add(auctionListing);
        database.Products.Add(product);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        var query = new ListingCatalogQuery(
            "iphone", "%iphone%", 1, 400, 600, "NEW", "Apple", "AUCTION", 2,
            null, true, 3, 10, ListingCatalogSort.PriceAscending, 1, 20);

        var result = await Repository(database).GetAvailableListingsAsync(query, Now, default);

        var item = Assert.Single(result.Items);
        Assert.Equal(12, item.ListingId);
        Assert.Equal(500, item.EffectivePrice);
        Assert.Equal(1, result.TotalItems);
    }

    [Fact]
    public async Task PriceSortIsStableAcrossFixedPriceAndAuctionListings()
    {
        await using var database = CreateDatabase();
        var product = NewProduct(1);
        var fixedListing = NewListing(11, 1, 500);
        var auctionListing = NewListing(12, 1);
        MakeAuction(auctionListing, 500);
        product.Listings.Add(fixedListing);
        product.Listings.Add(auctionListing);
        database.Products.Add(product);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        var query = new ListingCatalogQuery(
            null, null, null, null, null, null, null, null, null, null, null,
            null, null, ListingCatalogSort.PriceAscending, 1, 20);

        var result = await Repository(database).GetAvailableListingsAsync(query, Now, default);

        Assert.Equal(new[] { 11, 12 }, result.Items.Select(item => item.ListingId));
    }

    [Fact]
    public async Task ListingProjectionTranslatesWithTheSqlServerProviderWithoutNPlusOne()
    {
        var interceptor = new TranslationInterceptor();
        var options = new DbContextOptionsBuilder<CloneEbayDbContext>()
            .UseSqlServer("Server=unused.invalid;Database=TranslationOnly;Integrated Security=true;TrustServerCertificate=true")
            .AddInterceptors(interceptor)
            .Options;
        await using var database = new CloneEbayDbContext(options);

        await Assert.ThrowsAsync<TranslationReachedException>(() =>
            Repository(database).GetAvailableListingsAsync(1, 20, Now, default));

        Assert.Equal(2, interceptor.Commands.Count);
        Assert.Contains("COUNT(*)", interceptor.Commands[0]);
        Assert.Contains("ProductImage", interceptor.Commands[1]);
        Assert.Contains("Listing", interceptor.Commands[1]);
        Assert.Contains("ORDER BY", interceptor.Commands[1]);
        Assert.Contains("OFFSET", interceptor.Commands[1]);
    }

    private static ListingCatalogRepository Repository(CloneEbayDbContext database) => new(database);

    private static CloneEbayDbContext CreateDatabase() => new(
        new DbContextOptionsBuilder<CloneEbayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Product NewProduct(int id) => new() { id = id, title = $"Product {id}", categoryId = 1 };

    private static User NewSeller(int id, string username) => new()
    {
        id = id,
        username = username,
        email = $"{username}@example.test",
        role = "SELLER",
        accountStatus = "ACTIVE"
    };

    private static Product ProductWithListing(int id, Action<Listing>? configure = null)
    {
        var product = NewProduct(id);
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
        quantity = 1,
        createdAt = Now.AddDays(-1)
    };

    private static void MakeAuction(Listing listing, decimal currentPrice = 80)
    {
        listing.listingType = "AUCTION";
        listing.price = null;
        listing.Auction = new Auction
        {
            id = listing.id,
            listingId = listing.id,
            startingPrice = 50,
            currentPrice = currentPrice,
            bidIncrement = 1,
            bidCount = 2,
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
            if (Commands.Count > 1) throw new TranslationReachedException();

            var table = new DataTable();
            table.Columns.Add("Count", typeof(int));
            table.Rows.Add(1);
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }

    private sealed class TranslationReachedException : Exception;
}
