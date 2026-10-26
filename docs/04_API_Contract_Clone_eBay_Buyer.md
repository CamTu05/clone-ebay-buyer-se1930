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
| Auth | POST | `/api/auth/login` | Public | P0 | PROPOSED | TBD |
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
| Catalog | GET | `/api/products` | Public | P0 | PROPOSED | TBD |
| Catalog | GET | `/api/products/{productId}` | Public | P0 | PROPOSED | TBD |
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

**Guest checkout** không có account-linked order history; cần quyết định cách guest xem lại đơn (nếu có) bằng access token an toàn, không suy ra từ email. Cart guest ở local/session, không dùng bảng `Cart` của user đã login.

**Multi-seller:** `POST /api/orders` phải thể hiện `CheckoutGroup` và danh sách seller-specific orders trong response. Chưa chốt JSON cụ thể cho đến khi Owner hoàn thiện hợp đồng.

## 4. Feature-specific rules FE–BE phải giữ

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
