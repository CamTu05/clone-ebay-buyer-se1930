# 04. API CONTRACT — CLONE eBAY / BUYER

**Version:** 0.1 (Baseline / Draft)  
**Status:** Working contract — cập nhật cùng từng feature/PR  
**Ownership:** Tech Lead duyệt quy tắc chung; Feature Owner quản lý endpoint của mình  
**References:** 01 Requirements & Task Breakdown; 02 Architecture & Conventions; 03 Database Design; `clone_ebay_complete_schema_v2.sql`.

> **Quan trọng:** Đây là tài liệu thống nhất **quy ước** và **danh mục endpoint đề xuất**, không tuyên bố các API đã được implement. Tất cả endpoint trong phần *Endpoint Register* ở trạng thái `PROPOSED` cho đến khi được xác nhận qua PR. OpenAPI/Swagger của API chạy được sẽ là nguồn phản ánh chính xác request/response đã triển khai. Nếu contract thay đổi, update cùng PR.

## 1. Mục tiêu và phạm vi

- Giúp nhóm Fullstack thống nhất giao tiếp FE–BE trong 2 tuần; mỗi thành viên thiết kế request/response và implement cả hai phía cho feature mình phụ trách.
- Scope chỉ gồm **Buyer**: Account, Profile, Address, Catalog/Listing, Cart, Checkout (authenticated + guest), Shipping/Tax preview, Payment mock, Order, Auction/Proxy Bid/SignalR, và feature P1 như Coupon, Return, Review, Watchlist, Notification.
- Không tự triển khai Seller/Admin đầy đủ; dữ liệu có thể lấy từ DB demo/mock integration theo abstraction.
- **Các thực thể theo DB v2:** `Product` khác `Listing` (giá, seller, buying format nằm ở `Listing`); `CheckoutGroup` gồm nhiều `OrderTable` theo seller; key nhiều bảng là `INT` hoặc `BIGINT`, **không mặc định dùng Guid**.

## 2. Conventions đã chốt trong tài liệu 02

| Quy tắc | Chuẩn |
|---|---|
| Base route | `/api/<resource>` (chưa thêm `/v1`; nếu nhóm muốn versioning, phải duyệt thay đổi chung) |
| Phương thức | GET đọc không gây side effect; POST tạo/command; PATCH cập nhật một phần; DELETE xóa liên kết được phép |
| JSON | `application/json`, camelCase |
| Response chung | `{ "success": true, "message": "Success", "data": ..., "errors": null }` |
| Error chung | `{ "success": false, "message": "...", "data": null, "errors": [{"code":"...", "field":"..."}] }` |
| Pagination | `page` từ 1, `pageSize` mặc định 20, tối đa 100 |
| Pagination data | `{ "items": [], "page": 1, "pageSize": 20, "totalItems": 0, "totalPages": 0 }` |
| HTTP | 200, 201, 204, 400, 401, 403, 404, 409, 500 (thêm 429 cho rate limiting) |
| Security | Bearer JWT cho protected endpoints; auth/ownership validation ở server |
| Money/time | `decimal(18,2)` trong DB, client không được gửi finalTotal/tax/shippingFee; UTC timestamp |
| DTO | `VerbNounRequest`, `NounResponse`, không expose EF entities |

**Quy ước đề xuất cần confirm khi triển khai:** Với 204 trả body rỗng (không dùng JSON envelope). Các lỗi validation có thể dùng 400; exception middleware map error code ổn định theo hệ thống. Mỗi request có `X-Correlation-ID` (tùy chọn). FE không được tin `success=true` nếu HTTP status báo lỗi.

### Response mẫu

**200:**
```json
{
  "success": true,
  "message": "Success",
  "data": {"items": [], "page": 1, "pageSize": 20, "totalItems": 0, "totalPages": 0},
  "errors": null
}
```

**409:**
```json
{
  "success": false,
  "message": "Auction is no longer active",
  "data": null,
  "errors": [{"code": "AUCTION_ENDED", "field": "auctionId"}]
}
```

## 3. Endpoint Register (đề xuất, CHƯA chốt request/response)

`Owner` lấy từ Jira/task board. Khi feature owner bắt đầu code: cập nhật phương thức/path/schema/error/auth và chuyển `PROPOSED → AGREED → IMPLEMENTED → TESTED`.

| Feature | Method | Endpoint (draft) | Auth | Priority | Status | Owner |
|---|---|---|---|---|---|---|
| Auth | POST | `/api/auth/register` | Public | P0 | PROPOSED | TBD |
| Auth | POST | `/api/auth/verify-email` | Public/token | P0 | PROPOSED | TBD |
| Auth | POST | `/api/auth/login` | Public | P0 | TESTED | camtu |
| Auth | POST | `/api/auth/logout` | JWT | P0 | PROPOSED | TBD |
| Auth/OTP | POST | `/api/auth/otp/send` | TBD | P0 | PROPOSED | TBD |
| Auth/OTP | POST | `/api/auth/otp/verify` | TBD | P0 | PROPOSED | TBD |
| Auth/Social | GET | `/api/auth/external/{provider}/start` | Public | P0* | PROPOSED | TBD |
| Auth/Social | GET | `/api/auth/external/{provider}/callback` | Public | P0* | PROPOSED | TBD |
| Profile | GET | `/api/me` | JWT | P0 | PROPOSED | TBD |
| Profile | PATCH | `/api/me` | JWT | P0 | PROPOSED | TBD |
| Address | GET/POST | `/api/addresses` | JWT | P0 | PROPOSED | TBD |
| Address | PATCH/DELETE | `/api/addresses/{addressId}` | JWT/owner | P0 | PROPOSED | TBD |
| Address | PATCH | `/api/addresses/{addressId}/default` | JWT/owner | P0 | PROPOSED | TBD |
| Catalog | GET | `/api/products` | Public | P0 | TESTED | MQuynh |
| Catalog | GET | `/api/products/{productId}` | Public | P0 | IMPLEMENTED | TBD |
| Listing | GET | `/api/listings/{listingId}` | Public | P0 | PROPOSED | TBD |
| Cart | GET | `/api/cart` | JWT | P0 | PROPOSED | TBD |
| Cart | POST | `/api/cart/items` | JWT | P0 | PROPOSED | TBD |
| Cart | PATCH/DELETE | `/api/cart/items/{cartItemId}` | JWT/owner | P0 | PROPOSED | TBD |
| Cart | POST | `/api/cart/merge` | JWT | P0 | PROPOSED | TBD |
| Checkout | POST | `/api/checkout/preview` | Guest/JWT | P0 | PROPOSED | TBD |
| Checkout | POST | `/api/orders` | Guest/JWT | P0 | PROPOSED | TBD |
| Order | GET | `/api/orders` | JWT | P0 | PROPOSED | TBD |
| Order | GET | `/api/orders/{orderId}` | JWT/owner | P0 | PROPOSED | TBD |
| Auction | GET | `/api/auctions/{auctionId}` | Public | P0 | PROPOSED | TBD |
| Auction | POST | `/api/auctions/{auctionId}/bids` | JWT/eligible | P0 | PROPOSED | TBD |
| Auction | GET | `/api/auctions/{auctionId}/bids` | Public/JWT (redacted) | P0 | PROPOSED | TBD |
| Coupon | POST | `/api/coupons/validate` | Guest/JWT | P1 | PROPOSED | TBD |
| Order Cancel | POST | `/api/orders/{orderId}/cancel-request` | JWT/owner | P1 | PROPOSED | TBD |
| Return | POST | `/api/orders/{orderId}/returns` | JWT/owner | P1 | PROPOSED | TBD |
| Review | POST | `/api/reviews` | JWT/eligible | P1 | PROPOSED | TBD |
| Watchlist | GET | `/api/watchlist` | JWT | P1 | PROPOSED | TBD |
| Watchlist | POST | `/api/watchlist` | JWT | P1 | PROPOSED | TBD |
| Watchlist | DELETE | `/api/watchlist/{listingId}` | JWT | P1 | PROPOSED | TBD |
| Notification | GET | `/api/notifications` | JWT | P1 | PROPOSED | TBD |
| Notification | PATCH | `/api/notifications/{id}/read` | JWT/owner | P1 | PROPOSED | TBD |

`P0*`: Social Login được ghi P0 trong scope nhưng tài liệu 01 mục Out of Scope cho phép hoãn khi thiếu thời gian; cần xác nhận ưu tiên trước sprint. **Không triển khai toàn bộ register/login/external flow từ bảng này khi chưa chốt schema và bảo mật.**

### 3.1 Auth/Login — Implemented contract

#### `POST /api/auth/login`

| Field | Value |
|---|---|
| Feature | Buyer email/password login |
| Authentication | Public; dedicated fixed-window rate limit, default 5 requests/60 seconds per remote IP |
| Request | `LoginRequest` with required `email` and `password` |
| Success | `200 OK` with `LoginResponse` in the standard envelope |
| Validation | `400 Bad Request`, `VALIDATION_ERROR` |
| Invalid credentials | `401 Unauthorized`, `AUTH_INVALID_CREDENTIALS` |
| Restricted account | `403 Forbidden`, `AUTH_ACCOUNT_UNAVAILABLE` |
| Email not verified | `403 Forbidden`, `AUTH_EMAIL_NOT_VERIFIED` |
| Rate limited | `429 Too Many Requests`, `AUTH_RATE_LIMITED`, with `Retry-After` when available |

Request:

```json
{
  "email": "buyer@example.com",
  "password": "user-supplied-password"
}
```

Success response:

```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "accessToken": "<jwt>",
    "tokenType": "Bearer",
    "expiresAt": "2026-10-26T12:15:00Z",
    "user": {
      "id": 123,
      "username": "buyer-name",
      "email": "buyer@example.com",
      "role": "BUYER",
      "emailVerified": true,
      "phoneVerified": false
    }
  },
  "errors": null
}
```

Invalid-credentials response:

```json
{
  "success": false,
  "message": "Invalid email or password",
  "data": null,
  "errors": [
    { "code": "AUTH_INVALID_CREDENTIALS", "field": null }
  ]
}
```

Implemented policy assumptions:

- Login identifier is email only. Input is trimmed and normalized with invariant uppercase for lookup; registration must apply the same policy. The existing unique-email behavior remains dependent on the deployed SQL Server collation because no normalized-email column was added.
- Only users with `accountStatus = ACTIVE`, `role = BUYER`, and `emailVerified = true` receive a token. Phone verification is returned to the client but is not required for login.
- Unknown email, null password hash, and incorrect password produce the same public credentials error. Unknown-user requests still perform password-hash work to reduce timing-based account enumeration.
- Password verification uses ASP.NET Core `IPasswordHasher<User>` against the existing database-first `dbo.User`; compatible older hashes are upgraded after successful verification.
- Access tokens use HS256, configured issuer/audience/external signing key, and a fixed 15-minute lifetime. Bearer validation checks signature, issuer, audience, expiration, and the allowed algorithm. Signing keys must come from environment variables or user-secrets through `Jwt__SigningKey` and are never committed.
- The frontend keeps the access token in memory only. Browser refresh ends the local session.
- No server-side refresh/session/revocation store exists. Logout clears the client token only; an already issued JWT remains valid until expiration. `/api/auth/logout` remains unimplemented and `PROPOSED` until revocation/session semantics are separately approved.
- Registration, email-verification issuance, phone OTP, and Google/Facebook login are not implemented or redesigned by this feature.

### 3.2 Catalog — Available product list

#### `GET /api/products?page=1&pageSize=20`

| Field | Value |
|---|---|
| Owner | MQuynh |
| Authentication | Public; existing API rate limit applies |
| Query | `page`: integer >= 1, default 1; `pageSize`: integer 1–100, default 20 |
| Success | `200 OK`, standard envelope with `ProductPageResponse` |
| Validation | `400 Bad Request`, `VALIDATION_ERROR` for invalid pagination |
| Rate limited | `429 Too Many Requests`, `RATE_LIMIT_EXCEEDED` |
| Unexpected failure | `500 Internal Server Error`, `INTERNAL_ERROR`; no database/exception details in response |
| Frontend | `/catalog`; URL pagination, loading, image fallback, empty/out-of-range state, error and retry |
| Verification | Repository/service tests, SQL Server query translation, HTTP success/validation/failure tests, frontend build/lint, browser checks with API fixtures |

Example success (sample data):

```json
{
  "success": true,
  "message": "Success",
  "data": {
    "items": [
      {
        "productId": 7,
        "listingId": 12,
        "name": "Wireless headphones",
        "price": 350000,
        "imageUrl": "https://example.com/headphones.jpg",
        "listingType": "FIXED_PRICE"
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1
  },
  "errors": null
}
```

Catalog projection and availability policy for this feature:

- One entry per distinct `Product` having an eligible `Listing`; ascending `productId` provides deterministic pagination. Count is calculated before paging. A page beyond the last returns `items: []` with the requested page and actual totals; an empty catalog has `totalItems: 0`, `totalPages: 0`.
- Eligible listings have `status = ACTIVE`, `quantity > 0`, no future `publishedAt`, and no elapsed `endedAt`. A fixed-price listing requires a non-null `price` (zero is valid). An auction requires its related `Auction` to be `ACTIVE` with `startTime <= UTC now < endTime`; its display price is `currentPrice`.
- Choose the lowest eligible display price, breaking ties by `listingId`; return both IDs and the chosen `listingType`. The UI labels auction prices as **Current bid**, not as a fixed purchase price.
- Pick one product image by `isPrimary DESC, sortOrder ASC, id ASC`. `imageUrl` is nullable. Missing, invalid, or failed images display a local placeholder.
- The UI formats prices as VND, following existing checkout/order currency defaults. Product/Listing has no currency column; this is the catalog's current display assumption.
- `Listing.quantity` is a read-only catalog availability assumption, not a resolution of the documented `Inventory` stock-authority question. No stock mutations, reservation, purchase guarantees, or schema changes are introduced. Checkout must independently validate stock and price.
- SQL Server is required for catalog data; missing configuration or database failures use the standard error path rather than a fake empty catalog. Supply `ConnectionStrings__BuyerDatabase` and the existing JWT environment configuration when running the API.

**Guest checkout** không có account-linked order history; cần quyết định cách guest xem lại đơn (nếu có) bằng access token an toàn, không suy ra từ email. Cart guest ở local/session, không dùng bảng `Cart` của user đã login.

**Multi-seller:** `POST /api/orders` phải thể hiện `CheckoutGroup` và danh sách seller-specific orders trong response. Chưa chốt JSON cụ thể cho đến khi Owner hoàn thiện hợp đồng.

## 4. Feature-specific rules FE–BE phải giữ

Product list/detail request, response, visibility and error contracts are documented in [06-product-details.md](06-product-details.md). The Product/Listing projection there is the implemented choice for team review; database-backed acceptance verification is still required.

### 4.1 Listing / Catalog
- Phân biệt rõ `productId` và `listingId` trong URL/DTO. Cart, bidding, shipping option phụ thuộc **listing**.
- Search/filter/sort/page ở server. `pageSize <= 100`; không trả maximumBid của bidder khác.

### 4.2 Cart / Checkout / Shipping / Tax
- Item trong Cart **không giữ hàng**; server revalidate listing price/stock ở checkout.
- Checkout quote tính lại subtotal, discount, shipping, tax, importCharge và total; **không nhận giá cuối do FE tính**.
- Shipping `FREE`, `FLAT_RATE`, `CALCULATED`; tax theo `TaxRegion` giả lập. Đổi address/option phải recalculation.
- Authenticated và guest checkout đều là P0; guest cung cấp email + địa chỉ giao hàng.
- Một lần checkout có thể tạo nhiều đơn theo seller: `CheckoutGroup` → `OrderTable` → `OrderItem`.
- COD tạo payment `PENDING`, không phải `PAID`; order status tách biệt payment status.
- Dữ liệu shipping address, title, unitPrice, fees, tax được snapshot trong đơn.

### 4.3 Auction & SignalR
- Authenticated/verified/eligible buyer mới đặt bid.
- API nhận **maximumBid**; server tính `effectiveBid` và `currentPrice` theo proxy bidding rule.
- Equal maximumBid: bid hợp lệ đến trước ưu tiên.
- Concurrency: transaction + `Auction.rowVersion`; không nhận bid sau `endTime`.
- Chỉ broadcast SignalR sau commit; không expose bid maximum của người khác.
- Hub route **đề xuất**: `/hubs/auctions`; group per `auctionId`.
- Event đã được tài liệu 02 đặt tên: `AuctionUpdated` (`auctionId`, `currentPrice`, `bidCount`, `highestBidderChanged`, `serverTime`) và `AuctionEnded` (`auctionId`, `winnerId`/masked winner, `finalPrice`). Confirm nullable/privacy trước implement.

### 4.4 Authorization
- Các endpoint theo buyer-owned resource phải kiểm tra owner từ JWT, **không tin userId/buyerId từ request body**.
- Không lộ payment secrets, hashed tokens, OTP, JWT và private maximum bid trong logs/response.

## 5. Mẫu mô tả một endpoint — Feature Owner copy khi làm

### `[METHOD] /api/[resource]`

| Field | Value |
|---|---|
| Feature / Requirement ID | `TBD` |
| Owner | `TBD` |
| Priority / Status | `P0 / PROPOSED` |
| Authentication / Ownership | `TBD` |
| Purpose | `TBD` |
| Request headers | `Content-Type: application/json`, `Authorization` nếu cần |
| Path / Query parameters | `TBD` |
| Request DTO | `TBD` — tên C# + JSON sample |
| Success status + Response DTO | `TBD` — 200/201/204 + JSON sample |
| Error cases | `TBD` — HTTP status + error code |
| Data entities / integrations | `TBD` |
| Related business rules | `BR-...` |
| Test cases | `TBD` |
| Swagger/OpenAPI updated | `[ ]` |
| Frontend integrated | `[ ]` |

**Hướng dẫn:** Thêm ví dụ request/response thực tế có giá trị kiểm thử, không điền schema suy đoán. Cập nhật endpoint này trước hoặc cùng PR. Swagger/OpenAPI của mã nguồn phải khớp với tài liệu.

## 6. Một số hợp đồng cần nhóm xác nhận ngay

| Vấn đề | Phát hiện từ tài liệu | Việc cần chốt |
|---|---|---|
| Authentication / DB | SQL v2 dùng `dbo.User(id INT, passwordHash...)`; prompt khởi tạo trước dự kiến ASP.NET Identity mặc định (`AspNetUsers`) | Chọn Identity có custom store/mapping hoặc mô hình auth phù hợp; không tạo hai hệ user không đồng bộ |
| API prefix | Tài liệu 02 dùng `/api/...`; prompt khởi tạo cũ dùng `/api/v1/...` | Chọn **một** route convention; bản draft dùng `/api/...` |
| Social login priority | Tài liệu 01 vừa ghi P0 vừa cho hoãn khi không đủ thời gian; DB tài liệu 03 xếp `ExternalLogin` P2 | Chốt mức ưu tiên trước phân việc |
| Endpoint naming | Tài liệu 02 gọi `/api/products`, DB v2 phân biệt Product/Listing | Chốt endpoint nào trả listing bán cụ thể, productId/listingId tương ứng |
| Auth guest | Guest tạo đơn không có tài khoản, nhưng schema có sellerId và shipping snapshot | Chốt guest-order read access an toàn và response split orders |
| API token/bid | JWT và SignalR cần auth riêng; bid maximum nhạy cảm | Chốt policy Hub, per-auction group và payload redaction |
| Schema migrations | File SQL v2 `DROP DATABASE` ở đầu | **Không** chạy script nguyên trạng lên DB có dữ liệu; thống nhất baseline EF migrations/SQL bootstrap |

## 7. Quy trình cập nhật tài liệu và PR

1. Feature Owner tạo/nhận Jira task và đọc BR/DB liên quan.
2. Trước khi code endpoint, thêm method/path + request/response + auth + errors vào file này (hoặc OpenAPI YAML), chuyển `PROPOSED → AGREED` qua review nhanh.
3. Implement Backend, FE, Swagger/OpenAPI và test trong branch cá nhân; không chỉnh API đã thống nhất âm thầm.
4. PR vào `develop` gồm code + contract nếu có thay đổi + schema/migration nếu có.
5. Review build, auth/ownership, test request success/error, kiểm tra contract với Swagger.
6. Sau merge, cập nhật trạng thái `IMPLEMENTED` hoặc `TESTED`.

**Definition of Done (API):** endpoint chạy được, đúng status/envelope/auth, Swagger cập nhật, FE gọi được, có ít nhất happy path và error path, không làm hỏng build/migration.

## 8. Phần chưa thực hiện (cố ý)

- Chưa khóa JSON request/response cho từng endpoint; Owner sẽ bổ sung khi thực hiện feature.
- Chưa tạo OpenAPI specification đầy đủ hay client SDK vì API chưa implement.
- Chưa mặc định tự tạo entity/migration từ bảng register; schema v2 và kỹ thuật ánh xạ auth cần chốt riêng.

---

**Lịch sử thay đổi**

| Version | Change | Owner |
|---|---|---|
| 0.1 | Baseline conventions + proposed endpoint register based on docs 01–03 and SQL v2 | Team (draft) |
