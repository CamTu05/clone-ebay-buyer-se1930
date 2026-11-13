/* Seed demo for D:/Project/CloneEbayBuyer.sql. Run AFTER schema in SSMS.
   No existing data is deleted. Re-running aborts if seed marker exists.
   Demo account password: Demo@12345 (ASP.NET Core Identity hash).
   Product images are served by buyer-web; payments/social identities are simulated only. */
USE [CloneEbayBuyer];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRAN;
 IF EXISTS(SELECT 1 FROM dbo.[User] WITH(UPDLOCK,HOLDLOCK) WHERE email=N'seed.buyer01@example.test')
  THROW 51001, 'Demo seed already exists. No changes made.', 1;
 DECLARE @now datetime2(3)=SYSUTCDATETIME(), @i int=1,@uid int,@sid int,@cat int,@pid int,@lid int,@policy int,@returnPolicy int,@option int,@cart int,@auction int;
   DECLARE @buyers TABLE(n int PRIMARY KEY,id int);
   DECLARE @sellers TABLE(n int PRIMARY KEY,id int,policyId int,returnPolicyId int,optionId int);
  DECLARE @cats TABLE(n int PRIMARY KEY,id int);
  DECLARE @list TABLE(n int PRIMARY KEY,id int,productId int,sellerId int,optionId int,price decimal(18,2),title nvarchar(255),imageUrl nvarchar(1000));
 WHILE @i<=12
 BEGIN
  INSERT dbo.[User](username,email,role,emailVerified) VALUES(CONCAT(N'Demo Buyer ',@i),CONCAT(N'seed.buyer',RIGHT('0'+CONVERT(varchar(2),@i),2),N'@example.test'),N'BUYER',1);
  SET @uid=SCOPE_IDENTITY(); INSERT @buyers VALUES(@i,@uid);
  INSERT dbo.Address(userId,fullName,phone,street,city,country,countryCode,isDefault) VALUES(@uid,CONCAT(N'Khách mẫu ',@i),N'0000000000',CONCAT(@i,N' Đường Demo (dữ liệu giả)'),N'Hồ Chí Minh',N'Vietnam',N'VN',1);
  INSERT dbo.Cart(userId) VALUES(@uid);
  INSERT dbo.Notification(userId,type,title,content) VALUES(@uid,N'SYSTEM',N'Chào mừng tài khoản demo',N'Dữ liệu giả phục vụ phát triển và kiểm thử.');
  SET @i+=1;
 END;
 INSERT dbo.[User](username,email,role,emailVerified) VALUES(N'Demo Admin',N'seed.admin@example.test',N'ADMIN',1);
 SET @i=1;
 WHILE @i<=4
 BEGIN
  INSERT dbo.[User](username,email,role,emailVerified) VALUES(CONCAT(N'Demo Store ',@i),CONCAT(N'seed.seller',@i,N'@example.test'),N'SELLER',1);
  SET @sid=SCOPE_IDENTITY();
   INSERT dbo.ShippingPolicy(sellerId,name) VALUES(@sid,N'Giao hàng demo'); SET @policy=SCOPE_IDENTITY();
   INSERT dbo.ShippingPolicyDestinationCountry(shippingPolicyId,countryCode) VALUES(@policy,N'VN');
   IF @i<=2 INSERT dbo.ShippingPolicyDestinationCountry(shippingPolicyId,countryCode) VALUES(@policy,N'US');
   INSERT dbo.ReturnPolicy(sellerId,name,returnsAccepted,freeReturns,returnWindowDays)
   VALUES(@sid,CASE WHEN @i=4 THEN N'Không nhận trả hàng' ELSE N'Đổi trả 30 ngày' END,CASE WHEN @i=4 THEN 0 ELSE 1 END,CASE WHEN @i=2 THEN 1 ELSE 0 END,CASE WHEN @i=4 THEN NULL ELSE 30 END); SET @returnPolicy=SCOPE_IDENTITY();
   INSERT dbo.ShippingOption(shippingPolicyId,serviceName,serviceType,flatRate,estimatedMinDays,estimatedMaxDays) VALUES(@policy,N'Standard demo',N'STANDARD',CASE WHEN @i=2 THEN 0 ELSE 30000 END,2,5); SET @option=SCOPE_IDENTITY();
   INSERT @sellers VALUES(@i,@sid,@policy,@returnPolicy,@option);
   INSERT dbo.ShippingOption(shippingPolicyId,serviceName,serviceType,flatRate,estimatedMinDays,estimatedMaxDays) VALUES(@policy,N'Express demo',N'EXPRESS',60000,1,2);
   IF @i=1 INSERT dbo.ShippingOption(shippingPolicyId,serviceName,serviceType,flatRate) VALUES(@policy,N'Local pickup demo',N'LOCAL_PICKUP',0);
  SET @i+=1;
 END;
 SET @i=1;
 WHILE @i<=6
 BEGIN
  INSERT dbo.Category(name,slug) VALUES(CHOOSE(@i,N'Laptops',N'Smartphones',N'Cameras',N'Fashion',N'Home & Garden',N'Collectibles'),CONCAT(N'seed-category-',@i));
  INSERT @cats VALUES(@i,SCOPE_IDENTITY()); SET @i+=1;
 END;
 SET @i=1;
 WHILE @i<=48
 BEGIN
  SELECT @cat=id FROM @cats WHERE n=1+(@i-1)%6;
   SELECT @sid=id,@policy=policyId,@returnPolicy=returnPolicyId,@option=optionId FROM @sellers WHERE n=1+(@i-1)%4;
   DECLARE @kind int=1+(@i-1)%6;
   DECLARE @title nvarchar(255)=CONCAT(CHOOSE(@kind,N'Dell Latitude 7420',N'Samsung Galaxy S23',N'Canon EOS R50',N'Adidas Ultraboost Light',N'Philips LED desk lamp',N'Vintage die-cast model'),N' - Mẫu ',@i);
   DECLARE @price decimal(18,2)=250000+@i*125000;
   DECLARE @imageUrl nvarchar(1000)=CONCAT(N'/images/products/',CHOOSE(@kind,N'laptop.webp',N'smartphone.webp',N'camera.webp',N'sneakers.webp',N'desk-lamp.webp',N'collectible.webp'));
   DECLARE @productDescription nvarchar(max)=CHOOSE(@kind,
    CONCAT(N'Laptop doanh nhân mỏng nhẹ, phù hợp học tập và làm việc.',CHAR(13),CHAR(10),N'CPU: Intel Core i5-1145G7',CHAR(13),CHAR(10),N'RAM: ',CHOOSE(1+@i%2,N'16 GB',N'32 GB'),N' DDR4',CHAR(13),CHAR(10),N'Lưu trữ: ',CHOOSE(1+@i%2,N'512 GB',N'1 TB'),N' NVMe SSD',CHAR(13),CHAR(10),N'Màn hình: 14 inch Full HD',CHAR(13),CHAR(10),N'Kết nối: Wi-Fi 6, Bluetooth 5.2, Thunderbolt 4',CHAR(13),CHAR(10),N'Hệ điều hành: Windows 11 Pro'),
    CONCAT(N'Điện thoại 5G màn hình AMOLED, camera đa ống kính.',CHAR(13),CHAR(10),N'Màn hình: 6.1 inch Dynamic AMOLED 2X 120 Hz',CHAR(13),CHAR(10),N'Bộ nhớ: ',CHOOSE(1+@i%2,N'128 GB',N'256 GB'),CHAR(13),CHAR(10),N'RAM: 8 GB',CHAR(13),CHAR(10),N'Camera sau: 50 MP + 12 MP + 10 MP',CHAR(13),CHAR(10),N'Pin: 3900 mAh, sạc nhanh 25 W',CHAR(13),CHAR(10),N'Màu sắc: ',CHOOSE(1+@i%3,N'Đen',N'Kem',N'Xanh lá')),
    CONCAT(N'Máy ảnh mirrorless nhỏ gọn dành cho người sáng tạo nội dung.',CHAR(13),CHAR(10),N'Cảm biến: APS-C CMOS 24.2 MP',CHAR(13),CHAR(10),N'Video: 4K 30 fps',CHAR(13),CHAR(10),N'Lấy nét: Dual Pixel CMOS AF II',CHAR(13),CHAR(10),N'Màn hình: cảm ứng xoay lật 3 inch',CHAR(13),CHAR(10),N'Kết nối: Wi-Fi, Bluetooth',CHAR(13),CHAR(10),N'Ngàm ống kính: Canon RF'),
    CONCAT(N'Giày chạy bộ đệm hoàn trả năng lượng, thân giày thoáng khí.',CHAR(13),CHAR(10),N'Kích cỡ: EU ',40+@i%5,CHAR(13),CHAR(10),N'Màu sắc: ',CHOOSE(1+@i%3,N'Core Black',N'Cloud White',N'Blue'),CHAR(13),CHAR(10),N'Chất liệu thân: Primeknit+',CHAR(13),CHAR(10),N'Đế giữa: Light BOOST',CHAR(13),CHAR(10),N'Đế ngoài: cao su Continental',CHAR(13),CHAR(10),N'Phù hợp: chạy đường trường hằng ngày'),
    CONCAT(N'Đèn bàn LED điều chỉnh linh hoạt, ánh sáng dịu mắt.',CHAR(13),CHAR(10),N'Công suất: 10 W',CHAR(13),CHAR(10),N'Quang thông: 600 lm',CHAR(13),CHAR(10),N'Nhiệt độ màu: 3000-6500 K',CHAR(13),CHAR(10),N'Độ sáng: 5 mức',CHAR(13),CHAR(10),N'Nguồn điện: USB-C 5 V',CHAR(13),CHAR(10),N'Chất liệu: nhôm và ABS'),
    CONCAT(N'Mô hình kim loại sưu tầm có chi tiết nội thất và bánh xe chuyển động.',CHAR(13),CHAR(10),N'Tỷ lệ: 1:43',CHAR(13),CHAR(10),N'Chất liệu: hợp kim die-cast và ABS',CHAR(13),CHAR(10),N'Kích thước hộp: 18 x 8 x 7 cm',CHAR(13),CHAR(10),N'Năm phát hành: ',2020+@i%5,CHAR(13),CHAR(10),N'Phiên bản: giới hạn ',500+@i,N' chiếc',CHAR(13),CHAR(10),N'Khuyến nghị: từ 14 tuổi'));
   DECLARE @listingDescription nvarchar(max)=CONCAT(N'Tình trạng: ',CASE WHEN @i%3=0 THEN N'đã qua sử dụng, hoạt động tốt, có dấu hiệu sử dụng nhẹ' ELSE N'mới 100%, nguyên hộp' END,N'.',CHAR(13),CHAR(10),N'Bao gồm: sản phẩm, phụ kiện tiêu chuẩn và hướng dẫn sử dụng.',CHAR(13),CHAR(10),N'Bảo hành người bán: ',CASE WHEN @i%3=0 THEN N'30 ngày' ELSE N'12 tháng' END,N'.',CHAR(13),CHAR(10),N'Đóng gói chống sốc và bàn giao cho đơn vị vận chuyển trong 24 giờ.');
   INSERT dbo.Product(title,description,categoryId,brand) VALUES(@title,@productDescription,@cat,CHOOSE(@kind,N'Dell',N'Samsung',N'Canon',N'Adidas',N'Philips',N'Minichamps'));
   SET @pid=SCOPE_IDENTITY();
   INSERT dbo.ProductImage(productId,imageUrl,isPrimary) VALUES(@pid,@imageUrl,1);
   INSERT dbo.Listing(productId,sellerId,categoryId,shippingPolicyId,returnPolicyId,title,description,itemCondition,itemCountryCode,listingType,price,quantity,status,publishedAt)
   VALUES(@pid,@sid,@cat,@policy,@returnPolicy,@title,@listingDescription,CASE WHEN @i%7=0 THEN NULL WHEN @i%3=0 THEN N'Used - Good' ELSE N'New' END,CASE WHEN @i%4=0 THEN N'US' ELSE N'VN' END,CASE WHEN @i>40 THEN N'AUCTION' ELSE N'FIXED_PRICE' END,CASE WHEN @i>40 THEN NULL ELSE @price END,CASE WHEN @i>40 THEN 1 ELSE 5+@i%20 END,N'ACTIVE',DATEADD(day,-2,@now));
   SET @lid=SCOPE_IDENTITY(); INSERT @list VALUES(@i,@lid,@pid,@sid,@option,@price,@title,@imageUrl);
   INSERT dbo.ListingImage(listingId,imageUrl,isPrimary) VALUES(@lid,@imageUrl,1);
   IF @i<=40 AND @i%5=0 INSERT dbo.ListingPromotion(listingId,promotionType,originalPrice,salePrice,startsAt,endsAt)
    VALUES(@lid,CASE WHEN @i%10=0 THEN N'DEAL' ELSE N'SALE' END,@price,ROUND(@price*0.85,2),DATEADD(day,-1,@now),DATEADD(day,7,@now));
  IF @i>40
  BEGIN
   SELECT @uid=id FROM @buyers WHERE n=1;
   INSERT dbo.Auction(listingId,startingPrice,currentPrice,bidIncrement,startTime,endTime,highestBidderId,bidCount,status) VALUES(@lid,@price,@price,50000,DATEADD(day,-1,@now),DATEADD(day,7,@now),@uid,1,N'ACTIVE');
   SET @auction=SCOPE_IDENTITY();
   INSERT dbo.Bid(auctionId,bidderId,maximumBid,effectiveBid,status) VALUES(@auction,@uid,@price+100000,@price,N'WINNING');
  END;
   SET @i+=1;
  END;
  SET @i=1;
  WHILE @i<=4
  BEGIN
   SELECT @sid=id,@policy=policyId,@returnPolicy=returnPolicyId FROM @sellers WHERE n=@i;
   SELECT @cat=id FROM @cats WHERE n=@i;
   SET @title=CHOOSE(@i,N'Custom gaming laptop build',N'Unlocked Android smartphone bundle',N'Handmade leather camera strap',N'Limited edition running shoes');
   SET @imageUrl=CONCAT(N'/images/products/',CHOOSE(@i,N'laptop.webp',N'smartphone.webp',N'camera.webp',N'sneakers.webp'));
   INSERT dbo.Listing(productId,sellerId,categoryId,shippingPolicyId,returnPolicyId,title,description,itemCondition,itemCountryCode,listingType,price,quantity,status,publishedAt)
   VALUES(NULL,@sid,@cat,@policy,@returnPolicy,@title,N'Standalone listing created directly by the seller without a catalog product link.',CASE WHEN @i=3 THEN N'Used - Good' ELSE N'New' END,CASE WHEN @i=4 THEN N'US' ELSE N'VN' END,N'FIXED_PRICE',450000+@i*350000,5+@i,N'ACTIVE',DATEADD(day,-1,@now));
   SET @lid=SCOPE_IDENTITY();
   INSERT dbo.ListingImage(listingId,imageUrl,isPrimary) VALUES(@lid,@imageUrl,1);
   SET @i+=1;
  END;
  INSERT dbo.CartItem(cartId,listingId,quantity,status)
 SELECT c.id,l.id,1,CASE WHEN l.n%2=0 THEN N'SAVED_FOR_LATER' ELSE N'ACTIVE' END FROM @buyers b JOIN dbo.Cart c ON c.userId=b.id CROSS JOIN @list l WHERE l.n IN(1,2,3);
 INSERT dbo.Watchlist(userId,listingId) SELECT b.id,l.id FROM @buyers b CROSS JOIN @list l WHERE l.n IN(4,41,42);
 INSERT dbo.Coupon(code,discountType,discountValue,startDate,endDate,maxUsage) VALUES(N'SEED10',N'PERCENT',10,DATEADD(day,-30,@now),DATEADD(day,60,@now),100);
 DECLARE @coupon int=SCOPE_IDENTITY(),@group bigint,@order bigint,@item bigint,@return bigint,@discount decimal(18,2);
 SELECT @uid=id FROM @buyers WHERE n=1;
 -- Eight completed/confirmed/pending orders, snapshot addresses, matching payments.
 SET @i=1;
 WHILE @i<=8
 BEGIN
   SELECT @lid=id,@pid=productId,@sid=sellerId,@option=optionId,@price=price,@title=title,@imageUrl=imageUrl FROM @list WHERE n=@i;
  SET @discount=CASE WHEN @i=1 THEN @price/10 ELSE 0 END;
  INSERT dbo.CheckoutGroup(buyerId,couponId,subtotal,discountAmount,shippingFee,totalAmount,status,createdAt,confirmedAt,paymentDeadline)
  VALUES(@uid,CASE WHEN @i=1 THEN @coupon END,@price,@discount,30000,@price-@discount+30000,CASE WHEN @i=8 THEN N'PENDING' ELSE N'CONFIRMED' END,DATEADD(day,-10,@now),CASE WHEN @i<8 THEN DATEADD(day,-10,@now) END,CASE WHEN @i=8 THEN DATEADD(hour,1,@now) END);
  SET @group=SCOPE_IDENTITY();
  INSERT dbo.OrderTable(checkoutGroupId,buyerId,sellerId,shippingOptionId,shippingFullName,shippingEmail,shippingPhone,shippingStreet,shippingCity,shippingCountry,subtotal,discountAmount,shippingFee,totalAmount,status,orderDate)
  VALUES(@group,@uid,@sid,@option,N'Khách mẫu 1',N'seed.buyer01@example.test',N'0000000000',N'1 Đường Demo',N'Hồ Chí Minh',N'Vietnam',@price,@discount,30000,@price-@discount+30000,CASE WHEN @i<=4 THEN N'COMPLETED' WHEN @i=8 THEN N'PLACED' ELSE N'CONFIRMED' END,DATEADD(day,-10,@now));
  SET @order=SCOPE_IDENTITY();
   INSERT dbo.OrderItem(orderId,listingId,productId,quantity,unitPrice,productTitle,imageUrl,createdAt) VALUES(@order,@lid,@pid,1,@price,@title,@imageUrl,DATEADD(day,-10,@now)); SET @item=SCOPE_IDENTITY();
  UPDATE dbo.Listing SET quantity=quantity-1 WHERE id=@lid;
  INSERT dbo.Payment(checkoutGroupId,amount,method,provider,transactionId,status,paidAt,createdAt)
  VALUES(@group,@price-@discount+30000,N'PAYPAL',N'MOCK',CONCAT(N'SEED-PAY-',@group),CASE WHEN @i=8 THEN N'FAILED' ELSE N'PAID' END,CASE WHEN @i<8 THEN DATEADD(day,-10,@now) END,DATEADD(day,-10,@now));
  INSERT dbo.ShippingInfo(orderId,carrier,trackingNumber,status,shippedAt,deliveredAt)
  VALUES(@order,N'Demo Carrier',CONCAT(N'DEMO-',@order),CASE WHEN @i<=4 THEN N'DELIVERED' WHEN @i=5 THEN N'IN_TRANSIT' ELSE N'PENDING' END,CASE WHEN @i<=5 THEN DATEADD(day,-8,@now) END,CASE WHEN @i<=4 THEN DATEADD(day,-5,@now) END);
   IF @i<=5
   BEGIN
    INSERT dbo.Review(orderItemId,productId,reviewerId,rating,comment,createdAt)
    VALUES(@item,@pid,@uid,CHOOSE(@i,5,4,3,2,1),CHOOSE(@i,N'Sản phẩm đúng mô tả, đóng gói chắc chắn và hoạt động ổn định.',N'Chất lượng tốt, ngoại hình đẹp; thời lượng pin thấp hơn kỳ vọng một chút.',N'Sản phẩm dùng được nhưng hộp bị móp nhẹ khi giao tới.',N'Thông tin cơ bản đúng nhưng sản phẩm có nhiều vết xước hơn ảnh mô tả.',N'Sản phẩm gặp lỗi sau lần sử dụng đầu tiên và cần hỗ trợ đổi trả.'),DATEADD(day,-5+@i,@now));
    INSERT dbo.SellerFeedback(orderId,sellerId,buyerId,feedbackType,comment,createdAt)
    VALUES(@order,@sid,@uid,CHOOSE(@i,N'POSITIVE',N'POSITIVE',N'NEUTRAL',N'NEGATIVE',N'NEGATIVE'),CHOOSE(@i,N'Người bán phản hồi nhanh, giao đúng hạn và đóng gói cẩn thận.',N'Giao dịch thuận lợi, sản phẩm đúng mô tả.',N'Giao hàng chậm một ngày nhưng người bán có chủ động thông báo.',N'Phản hồi chậm và tình trạng thực tế không sát với mô tả.',N'Sản phẩm lỗi nhưng quá trình hỗ trợ sau bán hàng chưa tốt.'),DATEADD(day,-5+@i,@now));
   END;
   IF @i=4
  BEGIN
   INSERT dbo.ReturnRequest(orderId,requestedBy,reasonCode,reasonDetail) VALUES(@order,@uid,N'NOT_AS_DESCRIBED',N'Yêu cầu trả hàng minh họa'); SET @return=SCOPE_IDENTITY();
   INSERT dbo.ReturnRequestItem(returnRequestId,orderItemId,quantity) VALUES(@return,@item,1);
  END;
  IF @i=7 INSERT dbo.CancellationRequest(orderId,requestedBy,reason) VALUES(@order,@uid,N'Đặt nhầm sản phẩm demo');
  IF @i=1
  BEGIN
   INSERT dbo.CouponUsage(couponId,checkoutGroupId,userId,discountAmount) VALUES(@coupon,@group,@uid,@discount);
   UPDATE dbo.Coupon SET usageCount=1 WHERE id=@coupon;
  END;
   SET @i+=1;
  END;
  DECLARE @j int;
  SET @i=1;
  WHILE @i<=48
  BEGIN
   SELECT @lid=id,@pid=productId,@sid=sellerId,@option=optionId,@price=price,@title=title,@imageUrl=imageUrl FROM @list WHERE n=@i;
   SET @j=1;
   WHILE @j<=3
   BEGIN
    SELECT @uid=id FROM @buyers WHERE n=1+(@i+@j-2)%12;
    DECLARE @feedbackDate datetime2(3)=DATEADD(day,-(15+@i+@j*3),@now);
    INSERT dbo.CheckoutGroup(buyerId,subtotal,discountAmount,shippingFee,totalAmount,status,createdAt,confirmedAt)
    VALUES(@uid,@price,0,30000,@price+30000,N'CONFIRMED',DATEADD(day,-5,@feedbackDate),DATEADD(day,-5,@feedbackDate));
    SET @group=SCOPE_IDENTITY();
    INSERT dbo.OrderTable(checkoutGroupId,buyerId,sellerId,shippingOptionId,shippingFullName,shippingEmail,shippingPhone,shippingStreet,shippingCity,shippingCountry,subtotal,discountAmount,shippingFee,totalAmount,status,orderDate)
    VALUES(@group,@uid,@sid,@option,CONCAT(N'Khách mẫu ',1+(@i+@j-2)%12),CONCAT(N'seed.buyer',RIGHT('0'+CONVERT(varchar(2),1+(@i+@j-2)%12),2),N'@example.test'),N'0000000000',CONCAT(1+(@i+@j-2)%12,N' Đường Demo'),N'Hồ Chí Minh',N'Vietnam',@price,0,30000,@price+30000,N'COMPLETED',DATEADD(day,-5,@feedbackDate));
    SET @order=SCOPE_IDENTITY();
    INSERT dbo.OrderItem(orderId,listingId,productId,quantity,unitPrice,productTitle,imageUrl,createdAt)
    VALUES(@order,@lid,@pid,1,@price,@title,@imageUrl,DATEADD(day,-5,@feedbackDate));
    INSERT dbo.Payment(checkoutGroupId,amount,method,provider,transactionId,status,paidAt,createdAt)
    VALUES(@group,@price+30000,N'PAYPAL',N'MOCK',CONCAT(N'SEED-FEEDBACK-PAY-',@group),N'PAID',DATEADD(day,-5,@feedbackDate),DATEADD(day,-5,@feedbackDate));
    INSERT dbo.ShippingInfo(orderId,carrier,trackingNumber,status,shippedAt,deliveredAt)
    VALUES(@order,N'Demo Carrier',CONCAT(N'FEEDBACK-',@order),N'DELIVERED',DATEADD(day,-3,@feedbackDate),@feedbackDate);
    INSERT dbo.SellerFeedback(orderId,sellerId,buyerId,feedbackType,comment,createdAt)
    VALUES(@order,@sid,@uid,CASE WHEN (@i+@j)%13=0 THEN N'NEGATIVE' WHEN (@i+@j)%7=0 THEN N'NEUTRAL' ELSE N'POSITIVE' END,CASE WHEN (@i+@j)%13=0 THEN N'Sản phẩm giao chậm và người bán phản hồi chưa kịp thời.' WHEN (@i+@j)%7=0 THEN N'Sản phẩm đúng mô tả, thời gian giao hàng ở mức chấp nhận được.' WHEN @j=1 THEN N'Giao hàng nhanh, đóng gói chắc chắn và sản phẩm đúng mô tả.' WHEN @j=2 THEN N'Người bán thân thiện, hỗ trợ tốt và cập nhật đơn hàng đầy đủ.' ELSE N'Giao dịch thuận lợi, chất lượng sản phẩm tốt. Sẽ tiếp tục ủng hộ.' END,@feedbackDate);
    SET @j+=1;
   END;
   SET @i+=1;
   END;
   DECLARE @buyerA int=(SELECT id FROM dbo.[User] WHERE email=N'seed.buyer01@example.test');
   DECLARE @buyerB int=(SELECT id FROM dbo.[User] WHERE email=N'seed.buyer02@example.test');
   DECLARE @buyerC int=(SELECT id FROM dbo.[User] WHERE email=N'seed.buyer03@example.test');
   IF @buyerA IS NULL OR @buyerB IS NULL OR @buyerC IS NULL THROW 51003,N'Buyer feedback seed users are missing',1;
   ;WITH eligible AS (
    SELECT o.id,o.buyerId,o.sellerId,ROW_NUMBER() OVER(PARTITION BY o.buyerId ORDER BY o.orderDate,o.id) rn
    FROM dbo.OrderTable o JOIN dbo.ShippingInfo si ON si.orderId=o.id
    WHERE o.buyerId IN(@buyerA,@buyerB) AND o.status=N'COMPLETED' AND si.status=N'DELIVERED'
   )
   INSERT dbo.BuyerFeedback(orderId,buyerId,sellerId,comment,createdAt)
   SELECT id,buyerId,sellerId,CASE WHEN buyerId=@buyerA AND rn=1 THEN N'Great buyer, prompt payment. Thank you!' WHEN buyerId=@buyerA AND rn=2 THEN N'Excellent transaction, highly recommended buyer.' WHEN buyerId=@buyerA AND rn=3 THEN N'Fast payment and smooth transaction. Thank you.' ELSE N'Pleasure doing business with this buyer.' END,DATEADD(day,-rn,@now)
   FROM eligible WHERE (buyerId=@buyerA AND rn<=3) OR (buyerId=@buyerB AND rn=1);
   IF (SELECT COUNT(*) FROM dbo.BuyerFeedback WHERE buyerId=@buyerA)<>3 OR (SELECT COUNT(*) FROM dbo.BuyerFeedback WHERE buyerId=@buyerB)<>1 OR EXISTS(SELECT 1 FROM dbo.BuyerFeedback WHERE buyerId=@buyerC)
    THROW 51004,N'Buyer feedback seed counts are invalid',1;
   INSERT dbo.CheckoutGroup(guestEmail,subtotal,discountAmount,shippingFee,totalAmount,status,createdAt,paymentDeadline)
  VALUES(N'seed.guest@example.test',500000,0,30000,530000,N'PENDING',@now,DATEADD(day,1,@now));
  SET @group=SCOPE_IDENTITY();
  INSERT dbo.GuestCheckoutAccess(checkoutGroupId,tokenHash,expiresAt,createdAt)
  VALUES(@group,CONVERT(varchar(64),HASHBYTES('SHA2_256',N'seed-guest-access-token'),2),DATEADD(day,1,@now),@now);
  INSERT dbo.ExternalLogin(userId,provider,providerUserId) VALUES(@uid,N'GOOGLE',N'demo-not-a-real-google-identity');
 INSERT dbo.VerificationToken(userId,tokenType,tokenHash,createdAt,expiresAt)
 VALUES(@uid,N'PASSWORD_RESET',CONVERT(varchar(64),HASHBYTES('SHA2_256',CRYPT_GEN_RANDOM(32)),2),@now,DATEADD(hour,1,@now));
 IF EXISTS(SELECT 1 FROM dbo.CheckoutGroup g JOIN (SELECT checkoutGroupId,SUM(totalAmount) total FROM dbo.OrderTable GROUP BY checkoutGroupId) o ON o.checkoutGroupId=g.id WHERE g.buyerId=@uid AND g.totalAmount<>o.total)
 THROW 51002,'Order totals mismatch',1;
 -- Demo password hashes (verified with ASP.NET Core Identity).
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEAArdRExNwv0B5KyYOoNjHisWFfvoB5cpqMdTMVhPFr0OGEoxXjk8DyyXG5bt38MSQ==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer01@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEFvwlzVWTG55rEhs6f9Y1V/CUSfyPxQZUOZxo4702Zx66qs9x5ewYmj4BrCt0VXQUw==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer02@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEKiKZMv27hggaSIxFkWMolmssnNIS2sTQ8Jlezcoa1LyxxTElZNJ0Sk6B8SJnM5upg==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer03@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAENs48V1Za4+wqchkob+kFJooa9zSDR1OB8ArbNnpTlQjDM0CaRwiU75CjyLDtLYq0Q==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer04@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEL7uBmWVbY7GvUM8F0BJmHWeSu7WDME12ID2HEvEvDGYEJHs+/xINZgBcoE0y9TZww==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer05@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEEcdwGfh1n+/fDTuoUBO79o4JOA4rCo35DdYYmC9CYlD57P1QUD0DXpEN3c2YHYE6A==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer06@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAELVZDrelXfy+4B01Yt4aB4lMKUtbF82Kel0C4SxkWu+VZvLGnNxlIsaiczPRgRQXcw==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer07@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEBmkgb6XpMgUUDBxmHDXlYSQ5zfD+V6YGm7FPS35zsFc6m9yfxySlURZ5jMIXoo0NA==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer08@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAELAvv0Q696Lv5BDk/ZhAv4mk6wxjUvJ2XSwS1ilUngISNh1OQ8VATaO01+xPmvxoHA==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer09@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAECAnAP1Ha/yw5vL/cP5/jfFwuN8Q7XaHMJMkXmCcSpVDWxHQBdf4lzFFu/0hUyJ7Sw==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer10@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEFWZzjH20h3fEj+h2PMgg43VDvxHTgPEH4tzNYW3gmovnmpopJO6wofW3KCncMTreQ==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer11@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAELaPtEXia2Aig8h/K4SE6gpgbFymJ9U4ql0cHaaAQno4baYGTrrmR6x8+3pNPQMLew==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer12@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEP+eQopqihxHz+UY2NFmHRDYKGpvp72TdRL2LqePDxv83l/cNzdneV6DbaJ2961RwA==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.seller1@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEKZhr+m09Gxlj9vzbgtaza8gqYtN/9ivmmAoFq2qSDZh30sj6IwlkugKpHQhYqLiDA==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.seller2@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEGhPGKD1x6tKEa8eeo9dM6ZjewLNXPugQCNr0Qo1znnplfc1cxc+/OjZySFc29KKsw==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.seller3@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEPQSvrkLwdYGFYtiaHVowPTQb+zeMjbSwaC1UccNClNXwt7lQtNTEHGDvKeXOfJFVA==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.seller4@example.test' AND passwordHash IS NULL;
 UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEN6pS+O9ApCcrcRVHtljIOZ9j43/NOVPmbqIwj49321pedUJ4nvXVGhsZggpX1bLGw==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.admin@example.test' AND passwordHash IS NULL;
  INSERT dbo.OperationReceipt(scope,operationKey,requestHash,responseJson)
  VALUES(N'seed.demo',N'checkout-confirmation',CONVERT(varchar(64),HASHBYTES('SHA2_256',N'seed-checkout-confirmation'),2),N'{"status":"CONFIRMED","source":"demo-seed"}');
  INSERT dbo.EmailOutbox(recipient,subject,body,attempts)
  VALUES(N'seed.buyer01@example.test',N'Đơn hàng demo đã được xác nhận',N'Email minh họa trong hàng đợi gửi thư.',0);
  COMMIT;
 SELECT N'Demo seed completed' result, (SELECT COUNT(*) FROM @buyers) buyers,(SELECT COUNT(*) FROM @sellers) sellers,(SELECT COUNT(*) FROM @list) listings;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;
GO
