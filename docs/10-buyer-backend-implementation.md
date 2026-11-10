# Backend Buyer – triển khai trên CloneEbayBuyer

Ngày 10/11/2026. Đối chiếu `09-ebay-buyer-research.md` và schema thực tế `D:/Project/CloneEbayBuyer.sql`.

## Kết nối và chạy

- Database ứng dụng vẫn là **CloneEbayBuyer**. `BuyerSchema: V3` chỉ chọn mapping cho cấu trúc SQL hiện tại; không tạo database V3/V2.
- Giữ connection string bạn cung cấp trong `backend/Buyer.API/appsettings.json` (`DESKTOP-B7E8H2V\SQLEXPRESS`).
- `appsettings.Development.json` dùng `Server=.\SQLEXPRESS;Database=CloneEbayBuyer` để chạy trên SQL Express của máy hiện tại. Máy kiểm tra thực tế báo tên `DESKTOP-T60UPUR`; tên máy trong cấu hình gốc không kết nối được tại đây. Development ghi đè appsettings.json; nếu muốn dùng máy SQL khác, sửa Development hoặc đặt `ConnectionStrings__BuyerDatabase`.
- Không chạy lại script CREATE hoặc seed lên database đã có. Backend không tự tạo/reset database, không tự seed hoặc fallback dữ liệu mẫu.
- Cấu hình JWT SigningKey bằng user-secrets (ít nhất 32 ký tự), không commit khóa. Cấu hình hiện tại trên máy đã hoạt động.

```powershell
# Terminal 1, từ thư mục dự án
 dotnet run --project backend/Buyer.API --launch-profile http
# Terminal 2
 cd frontend/buyer-web
 npm run dev
```

API: http://localhost:5127; Swagger: http://localhost:5127/swagger; FE: http://localhost:5173.
`GET /health/ready` kiểm tra kết nối DB thật, trả 503 nếu không kết nối được.
Frontend đọc `VITE_API_BASE_URL`, mặc định http://localhost:5127. Phải khởi động lại backend cũ để dùng code mới.

## Các luồng đã nối SQL Server

| Luồng | Endpoint / quy tắc |
|---|---|
| Đăng nhập | POST `/api/auth/login`, hash mật khẩu Identity, active/verified buyer, JWT |
| Catalog | GET `/api/products`: page, pageSize, q, category (name/slug), condition, type, min, max, sort; tìm kiếm/lọc/phân trang trên server |
| Chi tiết | GET `/api/products/{id}`; listing, seller, condition, auctionId |
| Danh mục | GET `/api/categories` |
| Giỏ | GET `/api/cart`, POST `/api/cart/items`, PATCH/DELETE `/api/cart/items/{id}`; tồn kho lấy từ Listing.quantity |
| Địa chỉ | CRUD/default/resolve dưới `/api/addresses`; kiểm tra chủ sở hữu |
| Hồ sơ | GET/PATCH `/api/me`; avatar, thông tin thực trong User |
| Watchlist | GET `/api/watchlist`, PUT/DELETE `/api/watchlist/{listingId}`; đồng bộ tài khoản khi đăng nhập; guest vẫn lưu thiết bị |
| Checkout | POST `/api/checkout/preview`, POST `/api/checkout`; kiểm tra giỏ/địa chỉ/seller/stock, coupon, shipping; tách đơn theo seller; snapshot giá và địa chỉ |
| Thanh toán | POST `/api/checkout/{id}/mock-payment` với `{success:true/false}`; chỉ chủ sở hữu, không trả tiền trùng; **mô phỏng**, cần Payments:EnableMock=true |
| Hủy checkout | POST `/api/checkout/{id}/cancel`; chỉ checkout PENDING, hoàn stock đúng một lần |
| Đơn và tracking | GET `/api/orders`; dữ liệu ShippingInfo/trackingNumber trong DB, tối đa 100 đơn mới nhất |
| Yêu cầu hủy | POST `/api/orders/{id}/cancellation-requests`; chưa gửi/giao, chưa có yêu cầu đang chờ |
| Đánh giá | POST `/api/order-items/{id}/reviews`; hàng đã giao, một review/order item |
| Trả hàng | POST `/api/order-items/{id}/returns`; hàng đã giao, tổng lượng trả không vượt lượng mua |
| Thông báo | GET `/api/notifications`, PATCH `/api/notifications/{id}/read`; đúng tài khoản |
| Đấu giá | GET `/api/auctions/{id}`, POST `/api/auctions/{id}/bids`; proxy maximum bid, increment, tie theo thời gian/id; không công khai maximum của người khác |

Các giao dịch checkout/payment/bid dùng Serializable; cạnh tranh gây deadlock/unique conflict trả 409 để client tải lại và thử lại. Không giữ stock khi chỉ thêm giỏ. Checkout PayPal giữ stock 15 phút; COD tạo đơn CONFIRMED nhưng payment PENDING, không giả trạng thái đã thu tiền.

Worker `BuyerLifecycle:Enabled=true` xử lý mỗi 30 giây: checkout quá hạn hoàn tồn kho, auction đến hạn xác định winner. Đã bật trong Development; môi trường khác cần bật cấu hình này. Các test endpoint dùng mock/SQLite tắt worker để tách biệt dữ liệu. Khi tắt, checkout quá hạn không trả tiền được nhưng cần hủy để giải phóng stock.

## Giới hạn rõ ràng

- Không tích hợp thanh toán PayPal thật, thu COD, webhook, email, hãng vận chuyển, tax quốc tế hoặc refund thật. Phí ship là flatRate của option trong DB; một seller cần một shipping policy tương thích, mặc định chọn option rẻ nhất.
- Cancellation/return tạo yêu cầu chờ xử lý; duyệt yêu cầu/refund thuộc seller/admin, chưa triển khai ở module Buyer này.
- Auction đã có đặt giá và kết thúc; chưa chuyển winner thành checkout để thanh toán.
- Chưa có đăng ký/email verification, social login, guest checkout, Best Offer, saved searches, chat và bộ lọc thuộc tính chuyên biệt.
- Schema hiện tại thiếu lịch sử đổi username: giữ username bất biến để không bỏ qua quy tắc 30 ngày; sửa avatar vẫn hoạt động. Phone verification chưa có trường trong schema nên không xác nhận đã verify.
- Trang chủ vẫn dùng banner/ảnh thiết kế tĩnh. Catalog, giá, giỏ, checkout, orders và thông báo đọc API/DB. Không phải dữ liệu trực tiếp từ eBay.
- JWT giữ trong bộ nhớ theo thiết kế cũ; reload cần đăng nhập lại. Có thể tiếp tục checkout PENDING từ Purchase history.

## Kiểm thử

- `dotnet test EbayClone.sln -c Release --no-restore`: 68 unit + 74 integration tests passed.
- `npm run build`, `npm run lint`: passed.
- `scripts/buyer-api-smoke.mjs`: 61 assertions trên SQL Server riêng `CloneEbayBuyer_Test_20261110`: login/catalog/account/cart/watchlist/checkout/payment/cancel/bid/returns; kiểm chứng DB stock và paid record, race hai buyer mua đơn vị cuối.
- Database test được tạo riêng từ schema và seed; không reset/delete database ứng dụng. Script smoke có mutation; chỉ chạy với server trỏ đúng database test. Thiết lập BUYER_TEST_URL, BUYER_TEST_DATABASE (phải có prefix CloneEbayBuyer_Test_), BUYER_TEST_PASSWORD, tùy chọn BUYER_TEST_SQL_SERVER trước khi chạy Node. API test phải dùng cùng database. Khi seed bằng sqlcmd dùng `-I` để bật QUOTED_IDENTIFIER.
- Readiness test đã đổi sang mong đợi 503 khi DB không tồn tại; test address legacy chọn mapping Legacy rõ ràng để giữ SQLite fixtures. Smoke SQL kiểm tra mapping hiện tại thực tế.

Không coi build/API tests là kiểm chứng giao diện trình duyệt hoặc dịch vụ thanh toán thực.

### Kiểm tra bổ sung sau triển khai

- Script buyer-lifecycle-smoke.mjs đã chạy thành công 10 vòng checkout đồng thời, mỗi vòng đúng một HTTP 200 và một HTTP 409, tồn kho không âm.
- Kiểm tra worker tự expire checkout kể cả buyer đã bị khóa sau khi đặt hàng, hoàn tồn kho đúng một lần qua nhiều tick.
- Sửa nhận diện SqlException 1205 được EF bọc trong InvalidOperationException; lỗi cạnh tranh trả 409 thay vì 500.
- Danh mục sidebar đã đọc GET /api/categories từ DB; timeout FE tăng lên 20 giây cho giao dịch có chờ khóa SQL.

- Kiểm tra bản chạy thật phát hiện User Secrets ghi đè ConnectionStrings:BuyerDatabase sang CloneEbayDB_V2. Đã xóa riêng key ghi đè cũ bằng dotnet user-secrets remove; giữ JWT secret. Đây là nguyên nhân chỉ sửa appsettings trước đó vẫn kết nối sai DB.
