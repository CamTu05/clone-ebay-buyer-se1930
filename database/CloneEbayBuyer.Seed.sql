/* Seed demo for D:/Project/CloneEbayBuyer.sql. Run AFTER schema in SSMS.
   No existing data is deleted. Re-running aborts if seed marker exists.
   Demo account password: Demo@12345 (ASP.NET Core Identity hash).
   Images are placeholders; payments/social identities are simulated only. */
USE [CloneEbayBuyer];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRAN;
 IF EXISTS(SELECT 1 FROM dbo.[User] WITH(UPDLOCK,HOLDLOCK) WHERE email=N'seed.buyer01@example.test')
  THROW 51001, 'Demo seed already exists. No changes made.', 1;
 DECLARE @now datetime2(3)=SYSUTCDATETIME(), @i int=1,@uid int,@sid int,@cat int,@pid int,@lid int,@policy int,@option int,@cart int,@auction int;
 DECLARE @buyers TABLE(n int PRIMARY KEY,id int);
 DECLARE @sellers TABLE(n int PRIMARY KEY,id int,policyId int,optionId int);
 DECLARE @cats TABLE(n int PRIMARY KEY,id int);
 DECLARE @list TABLE(n int PRIMARY KEY,id int,productId int,sellerId int,optionId int,price decimal(18,2),title nvarchar(255));
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
  INSERT dbo.ShippingPolicy(sellerId,name) VALUES(@sid,N'Giao hàng nội địa demo'); SET @policy=SCOPE_IDENTITY();
  INSERT dbo.ShippingOption(shippingPolicyId,serviceName,serviceType,flatRate,estimatedMinDays,estimatedMaxDays) VALUES(@policy,N'Standard demo',N'STANDARD',30000,2,5); SET @option=SCOPE_IDENTITY();
  INSERT @sellers VALUES(@i,@sid,@policy,@option);
  INSERT dbo.ShippingOption(shippingPolicyId,serviceName,serviceType,flatRate,estimatedMinDays,estimatedMaxDays) VALUES(@policy,N'Express demo',N'EXPRESS',60000,1,2);
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
  SELECT @sid=id,@policy=policyId,@option=optionId FROM @sellers WHERE n=1+(@i-1)%4;
  DECLARE @title nvarchar(255)=CONCAT(CHOOSE(1+(@i-1)%6,N'Dell Latitude laptop',N'Samsung Galaxy phone',N'Canon EOS camera',N'Adidas sneakers',N'Philips desk lamp',N'Vintage collectible model'),N' Demo ',@i);
  DECLARE @price decimal(18,2)=250000+@i*125000;
  INSERT dbo.Product(title,description,categoryId,brand) VALUES(@title,N'Sản phẩm minh họa. Thông số và giá chỉ phục vụ kiểm thử.',@cat,CHOOSE(1+(@i-1)%6,N'Dell',N'Samsung',N'Canon',N'Adidas',N'Philips',N'Generic'));
  SET @pid=SCOPE_IDENTITY();
  INSERT dbo.ProductImage(productId,imageUrl,isPrimary) VALUES(@pid,CONCAT(N'https://placehold.co/600x600/png?text=Demo+',@i),1);
  INSERT dbo.Listing(productId,sellerId,categoryId,shippingPolicyId,title,description,itemCondition,listingType,price,quantity,status,publishedAt)
  VALUES(@pid,@sid,@cat,@policy,@title,N'Demo listing',CASE WHEN @i%3=0 THEN N'Used' ELSE N'New' END,CASE WHEN @i>40 THEN N'AUCTION' ELSE N'FIXED_PRICE' END,CASE WHEN @i>40 THEN NULL ELSE @price END,CASE WHEN @i>40 THEN 1 ELSE 20 END,N'ACTIVE',DATEADD(day,-2,@now));
  SET @lid=SCOPE_IDENTITY(); INSERT @list VALUES(@i,@lid,@pid,@sid,@option,@price,@title);
  INSERT dbo.ListingImage(listingId,imageUrl,isPrimary) VALUES(@lid,CONCAT(N'https://placehold.co/600x600/png?text=Demo+',@i),1);
  IF @i>40
  BEGIN
   SELECT @uid=id FROM @buyers WHERE n=1;
   INSERT dbo.Auction(listingId,startingPrice,currentPrice,bidIncrement,startTime,endTime,highestBidderId,bidCount,status) VALUES(@lid,@price,@price,50000,DATEADD(day,-1,@now),DATEADD(day,7,@now),@uid,1,N'ACTIVE');
   SET @auction=SCOPE_IDENTITY();
   INSERT dbo.Bid(auctionId,bidderId,maximumBid,effectiveBid,status) VALUES(@auction,@uid,@price+100000,@price,N'WINNING');
  END;
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
  SELECT @lid=id,@pid=productId,@sid=sellerId,@option=optionId,@price=price,@title=title FROM @list WHERE n=@i;
  SET @discount=CASE WHEN @i=1 THEN @price/10 ELSE 0 END;
  INSERT dbo.CheckoutGroup(buyerId,couponId,subtotal,discountAmount,shippingFee,totalAmount,status,createdAt,confirmedAt,paymentDeadline)
  VALUES(@uid,CASE WHEN @i=1 THEN @coupon END,@price,@discount,30000,@price-@discount+30000,CASE WHEN @i=8 THEN N'PENDING' ELSE N'CONFIRMED' END,DATEADD(day,-10,@now),CASE WHEN @i<8 THEN DATEADD(day,-10,@now) END,CASE WHEN @i=8 THEN DATEADD(hour,1,@now) END);
  SET @group=SCOPE_IDENTITY();
  INSERT dbo.OrderTable(checkoutGroupId,buyerId,sellerId,shippingOptionId,shippingFullName,shippingEmail,shippingPhone,shippingStreet,shippingCity,shippingCountry,subtotal,discountAmount,shippingFee,totalAmount,status,orderDate)
  VALUES(@group,@uid,@sid,@option,N'Khách mẫu 1',N'seed.buyer01@example.test',N'0000000000',N'1 Đường Demo',N'Hồ Chí Minh',N'Vietnam',@price,@discount,30000,@price-@discount+30000,CASE WHEN @i<=4 THEN N'COMPLETED' WHEN @i=8 THEN N'PLACED' ELSE N'CONFIRMED' END,DATEADD(day,-10,@now));
  SET @order=SCOPE_IDENTITY();
  INSERT dbo.OrderItem(orderId,listingId,productId,quantity,unitPrice,productTitle,createdAt) VALUES(@order,@lid,@pid,1,@price,@title,DATEADD(day,-10,@now)); SET @item=SCOPE_IDENTITY();
  UPDATE dbo.Listing SET quantity=quantity-1 WHERE id=@lid;
  INSERT dbo.Payment(checkoutGroupId,amount,method,provider,transactionId,status,paidAt,createdAt)
  VALUES(@group,@price-@discount+30000,N'PAYPAL',N'MOCK',CONCAT(N'SEED-PAY-',@group),CASE WHEN @i=8 THEN N'FAILED' ELSE N'PAID' END,CASE WHEN @i<8 THEN DATEADD(day,-10,@now) END,DATEADD(day,-10,@now));
  INSERT dbo.ShippingInfo(orderId,carrier,trackingNumber,status,shippedAt,deliveredAt)
  VALUES(@order,N'Demo Carrier',CONCAT(N'DEMO-',@order),CASE WHEN @i<=4 THEN N'DELIVERED' WHEN @i=5 THEN N'IN_TRANSIT' ELSE N'PENDING' END,CASE WHEN @i<=5 THEN DATEADD(day,-8,@now) END,CASE WHEN @i<=4 THEN DATEADD(day,-5,@now) END);
  IF @i<=3
  BEGIN
   INSERT dbo.Review(orderItemId,productId,reviewerId,rating,comment) VALUES(@item,@pid,@uid,5,N'Sản phẩm demo đúng mô tả.');
   INSERT dbo.SellerFeedback(orderId,sellerId,buyerId,feedbackType,comment) VALUES(@order,@sid,@uid,N'POSITIVE',N'Đánh giá minh họa.');
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
 COMMIT;
 SELECT N'Demo seed completed' result, (SELECT COUNT(*) FROM @buyers) buyers,(SELECT COUNT(*) FROM @sellers) sellers,(SELECT COUNT(*) FROM @list) listings;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;
GO
