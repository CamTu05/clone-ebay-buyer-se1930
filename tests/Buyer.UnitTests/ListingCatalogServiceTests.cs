using Buyer.Application.Interfaces.Authentication;
using Buyer.Application.Interfaces.ListingCatalog;

namespace Buyer.UnitTests;

public sealed class ListingCatalogServiceTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(int.MaxValue, 100, 21474837)]
    public async Task PaginationIncludesPartialLastPageAndHandlesEmptyCatalog(
        int totalItems, int pageSize, int expectedPages)
    {
        var repository = new StubListingCatalogRepository(new ListingCatalogPage([], totalItems));
        var service = new ListingCatalogService(repository, new FixedClock());

        var result = await service.GetListingsAsync(new ListingListRequest { Page = 2, PageSize = pageSize });

        Assert.Equal(2, result.Page);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(totalItems, result.TotalItems);
        Assert.Equal(expectedPages, result.TotalPages);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task RepositoryReceivesUtcSnapshotPaginationAndRequestCancellation()
    {
        var listing = new ListingSummaryResponse(
            8, 5, "Camera", null, null, null, 3, "FIXED_PRICE", 1500000m,
            false, false, null, null, null, null);
        var repository = new StubListingCatalogRepository(new ListingCatalogPage([listing], 21));
        var clock = new FixedClock();
        var service = new ListingCatalogService(repository, clock);
        using var cancellation = new CancellationTokenSource();

        var result = await service.GetListingsAsync(
            new ListingListRequest { Page = 3, PageSize = 10 }, cancellation.Token);

        Assert.Equal(3, repository.Query!.Page);
        Assert.Equal(10, repository.Query.PageSize);
        Assert.Equal(clock.UtcNow, repository.UtcNow);
        Assert.Equal(cancellation.Token, repository.CancellationToken);
        Assert.Equal(listing, Assert.Single(result.Items));
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task NormalizesFiltersEscapesLikeWildcardsAndChoosesDefaultSort()
    {
        var repository = new StubListingCatalogRepository(new ListingCatalogPage([], 0));
        var service = new ListingCatalogService(repository, new FixedClock());

        await service.GetListingsAsync(new ListingListRequest
        {
            Search = " 100%_[ ",
            Condition = " NEW ",
            Brand = " Brand ",
            ListingType = " auction ",
            FreeShipping = true,
            AllowBestOffer = false
        });

        Assert.NotNull(repository.Query);
        Assert.Equal("100%_[", repository.Query.Search);
        Assert.Equal("%100\\%\\_\\[%", repository.Query.SearchPattern);
        Assert.Equal("NEW", repository.Query.Condition);
        Assert.Equal("Brand", repository.Query.Brand);
        Assert.Equal("AUCTION", repository.Query.ListingType);
        Assert.Equal(ListingCatalogSort.Relevance, repository.Query.Sort);
        Assert.False(repository.Query.AllowBestOffer);
    }

    [Fact]
    public async Task BlankSearchUsesNewestButExplicitSortIsPreserved()
    {
        var repository = new StubListingCatalogRepository(new ListingCatalogPage([], 0));
        var service = new ListingCatalogService(repository, new FixedClock());

        await service.GetListingsAsync(new ListingListRequest { Search = "   ", Sort = "price_asc" });

        Assert.Null(repository.Query!.Search);
        Assert.Null(repository.Query.SearchPattern);
        Assert.Equal(ListingCatalogSort.PriceAscending, repository.Query.Sort);
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow { get; } = new(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);
    }

    private sealed class StubListingCatalogRepository(ListingCatalogPage result) : IListingCatalogRepository
    {
        public ListingCatalogQuery? Query { get; private set; }
        public DateTime UtcNow { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<ListingCatalogPage> GetAvailableListingsAsync(
            ListingCatalogQuery query,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            Query = query;
            UtcNow = utcNow;
            CancellationToken = cancellationToken;
            return Task.FromResult(result);
        }

        public Task<ListingDetailResponse?> GetAvailableListingAsync(
            int listingId,
            DateTime utcNow,
            CancellationToken cancellationToken) => Task.FromResult<ListingDetailResponse?>(null);
    }
}
