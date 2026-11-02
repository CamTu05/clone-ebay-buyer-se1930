# Buyer can view product details

Implementation scope: public product details and a minimal paginated catalog so buyers can open details from a list. Search, cart, checkout and bidding are separate tasks. No schema changes or seed scripts are required.

## Contract implemented for this feature

`GET /api/products?page=1&pageSize=20` returns the standard success envelope with `data: { items, page, pageSize, totalItems, totalPages }`. Each item contains `productId`, `listingId`, `name`, `price`, nullable `imageUrl`, and `listingType`. The list retains the availability and pricing rules in section 3.2 of `04_API_Contract_Clone_eBay_Buyer.md`; details use the public-listing rules below. Page starts at 1; pageSize is 1–100. Invalid pagination returns 400.

`GET /api/products/{productId}` returns the standard success envelope with:

```json
{
  "success": true,
  "message": "Success",
  "data": {
    "productId": 1,
    "name": "Camera",
    "description": "Camera description",
    "brand": "Example",
    "condition": "Used",
    "images": [{ "id": 1, "url": "https://example.com/camera.jpg", "isPrimary": true }],
    "listings": [{
      "listingId": 11,
      "listingType": "FIXED_PRICE",
      "price": 500000,
      "seller": { "id": 2, "username": "seller", "avatarUrl": null, "storeName": "Camera store" }
    }]
  },
  "errors": null
}
```

Projection choice for review: a Product has no single seller or price. Details return all ACTIVE listings belonging to ACTIVE sellers, ordered by listing ID. Fixed-price listings use `Listing.price`; auction listings use `Auction.currentPrice`, labelled as current bid. A missing price remains null and the UI displays “Price unavailable”. Currency is VND, matching this project's SQL defaults; this is not a multi-currency feature.

Only products in active categories with at least one such public listing are visible. Missing or non-public products return 404 `PRODUCT_NOT_FOUND`. A non-integer, out-of-range or non-positive product ID returns 400 `PRODUCT_ID_INVALID`. A missing database connection returns 503 `CATALOG_UNAVAILABLE`. Unexpected catalog failures return 500 `INTERNAL_ERROR`; throttling returns 429 `RATE_LIMIT_EXCEEDED`. Errors use `success: false`, `data: null`, and `errors: [{code, field}]`.

Images are ordered by primary image first, then sortOrder and ID. Frontend accepts HTTP(S) image URLs and provides a fallback for missing/broken images. Description is rendered as plain text, not HTML. Seller email, phone, password, tokens and private bid maximums are never projected.

## Architecture

React → Axios → ProductsController → ProductService → IProductRepository → ProductRepository → CloneEbayDbContext. EF access remains in Infrastructure. The application contracts expose DTOs, not scaffolded entities. If SQL is not configured, an unavailable repository keeps health endpoints usable while catalog requests report 503.

## Manual acceptance checks with an existing database

1. Configure `ConnectionStrings__BuyerDatabase` for the existing `CloneEbayDB_V2`, then start the API and frontend as documented in README.
2. Ensure the database already contains an active category, a product with image/description, an active seller and an ACTIVE listing linked to that product. Do not execute the destructive bootstrap SQL to obtain demo data.
3. Open `/catalog`, select a product, and verify `/products/{productId}` displays the backend name, image, description, listing price and seller.
4. Check multiple seller listings and auction current price if those records exist.
5. Visit `/products/abc`, `/products/0` and a nonexistent positive ID. Verify understandable errors and the back-to-catalog link.
6. Stop the API and retry: the page should show a load error and retry button. Navigate between products quickly to verify cancelled requests cannot overwrite the current page.
7. Verify empty images, broken image URLs, empty description and an empty catalog render their fallback states.

Automated API tests use a substituted repository and cover response fields, list-to-detail IDs, invalid IDs, not-found and invalid pagination. They do not establish connectivity or query execution against the existing SQL Server; those require the manual database checks above.
