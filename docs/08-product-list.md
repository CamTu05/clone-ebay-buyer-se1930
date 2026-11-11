# Buyer product list (EBAY-45)

## Acceptance criteria

- `GET /api/products?page=1&pageSize=20` is public and returns the success envelope with `items`, `page`, `pageSize`, `totalItems`, and `totalPages`.
- Each item includes product/listing IDs, name, price, nullable image URL, and listing type. Products are counted once; the cheapest eligible listing determines the price (listing ID breaks ties).
- Eligible listings have ACTIVE status, positive stock, an ACTIVE seller, and an active product category. Publication/end times apply; auctions must also be active and within their time window.
- Pagination defaults to 20 products, accepts 1–100 per page, and uses stable product ID ordering. Invalid parameters return 400. Out-of-range pages return empty items while preserving the total.
- The client displays name, VND price, image/fallback, current-bid labels, result counts, Previous/Next, and page-size selection. Both image and title open product details. Page state is stored in the URL.
- Loading, empty catalog, empty page, network/server errors, retry, and cancellation of stale requests are handled.
- Missing database configuration resolves to the unavailable repository and returns 503 `CATALOG_UNAVAILABLE`; unexpected errors return a generic 500 without internal details.

## eBay reference and scope

Compared with eBay's official search guidance: https://www.ebay.com/help/buying/search-tips/use-search?id=4006
and its result-page examples: https://developer.ebay.com/cms/files/connect-2020/aspect_mikecuttone.pdf

The implementation covers the product-list story, including result counts, product cards and navigation. Search, advanced filters, sorting, and watchlists are separate features; this is not a full reproduction of eBay search.

## Validation

- `dotnet test tests/Buyer.UnitTests/Buyer.UnitTests.csproj --no-restore`: 68 passed.
- Integration test suite: 74 passed, including pagination validation, empty results, safe 500/503 responses, and missing-database dependency resolution.
- Repository tests cover availability windows, category/seller visibility, cheapest eligible listing, stable pagination, missing images, and SQL Server query translation without opening a database connection.
- `npm.cmd run build`: passed.
- `npm.cmd run lint`: passed with two existing profile-component warnings.
- A live SQL Server query and browser interaction were not verified in this change.
