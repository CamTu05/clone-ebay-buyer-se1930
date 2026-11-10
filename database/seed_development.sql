SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now datetime2(3) = SYSUTCDATETIME();
DECLARE @PasswordHash nvarchar(500) = N'AQAAAAIAAYagAAAAEIRXIU5xRtrjSRO8FWH31/XA+zcLO85BBTejXSAPSiMmYeX/IePf1abDHEUHqpnyzA==';
DECLARE @BuyerId int;
DECLARE @SellerId int;
DECLARE @ElectronicsId int;
DECLARE @PhonesId int;
DECLARE @AudioId int;
DECLARE @CollectiblesId int;
DECLARE @ProductPhoneId int;
DECLARE @ProductHeadphonesId int;
DECLARE @ProductCameraId int;
DECLARE @ShippingPolicyId int;
DECLARE @PhoneListingId int;
DECLARE @HeadphonesListingId int;
DECLARE @CameraListingId int;
DECLARE @CartId int;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.[User]', N'U') IS NULL OR OBJECT_ID(N'dbo.Listing', N'U') IS NULL
        THROW 51000, 'CloneEbayDB_V2 schema is missing. Create the schema before running the seed.', 1;

    IF EXISTS (SELECT 1 FROM dbo.[User] WHERE email = N'buyer.dev@example.test')
        UPDATE dbo.[User]
        SET username = N'dev_buyer', passwordHash = @PasswordHash, role = N'BUYER',
            avatarURL = N'https://placehold.co/200x200?text=Buyer', phoneNumber = N'0900000001',
            emailVerified = 1, accountStatus = N'ACTIVE', updatedAt = @Now
        WHERE email = N'buyer.dev@example.test';
    ELSE
        INSERT dbo.[User] (username, email, passwordHash, role, avatarURL, phoneNumber, emailVerified, accountStatus, createdAt, updatedAt)
        VALUES (N'dev_buyer', N'buyer.dev@example.test', @PasswordHash, N'BUYER', N'https://placehold.co/200x200?text=Buyer', N'0900000001', 1, N'ACTIVE', @Now, @Now);

    SELECT @BuyerId = id FROM dbo.[User] WHERE email = N'buyer.dev@example.test';

    IF EXISTS (SELECT 1 FROM dbo.[User] WHERE email = N'seller.dev@example.test')
        UPDATE dbo.[User]
        SET username = N'dev_seller', passwordHash = @PasswordHash, role = N'SELLER',
            avatarURL = N'https://placehold.co/200x200?text=Seller', phoneNumber = N'0900000002',
            emailVerified = 1, accountStatus = N'ACTIVE', updatedAt = @Now
        WHERE email = N'seller.dev@example.test';
    ELSE
        INSERT dbo.[User] (username, email, passwordHash, role, avatarURL, phoneNumber, emailVerified, accountStatus, createdAt, updatedAt)
        VALUES (N'dev_seller', N'seller.dev@example.test', @PasswordHash, N'SELLER', N'https://placehold.co/200x200?text=Seller', N'0900000002', 1, N'ACTIVE', @Now, @Now);

    SELECT @SellerId = id FROM dbo.[User] WHERE email = N'seller.dev@example.test';

    IF EXISTS (SELECT 1 FROM dbo.Address WHERE userId = @BuyerId AND isDefault = 1)
        UPDATE dbo.Address
        SET fullName = N'Development Buyer', phone = N'0900000001', street = N'123 Nguyễn Huệ',
            addressLine2 = N'Quận 1', city = N'Hồ Chí Minh', state = N'Hồ Chí Minh',
            postalCode = N'700000', country = N'Việt Nam', countryCode = N'VN', updatedAt = @Now
        WHERE userId = @BuyerId AND isDefault = 1;
    ELSE
        INSERT dbo.Address (userId, fullName, phone, street, addressLine2, city, state, postalCode, country, countryCode, isDefault, createdAt, updatedAt)
        VALUES (@BuyerId, N'Development Buyer', N'0900000001', N'123 Nguyễn Huệ', N'Quận 1', N'Hồ Chí Minh', N'Hồ Chí Minh', N'700000', N'Việt Nam', N'VN', 1, @Now, @Now);

    IF NOT EXISTS (SELECT 1 FROM dbo.Category WHERE slug = N'dev-electronics')
        INSERT dbo.Category (name, slug, isActive) VALUES (N'Electronics', N'dev-electronics', 1);
    SELECT @ElectronicsId = id FROM dbo.Category WHERE slug = N'dev-electronics';
    UPDATE dbo.Category SET name = N'Electronics', isActive = 1 WHERE id = @ElectronicsId;

    IF NOT EXISTS (SELECT 1 FROM dbo.Category WHERE slug = N'dev-phones')
        INSERT dbo.Category (parentCategoryId, name, slug, isActive) VALUES (@ElectronicsId, N'Phones', N'dev-phones', 1);
    SELECT @PhonesId = id FROM dbo.Category WHERE slug = N'dev-phones';
    UPDATE dbo.Category SET parentCategoryId = @ElectronicsId, name = N'Phones', isActive = 1 WHERE id = @PhonesId;

    IF NOT EXISTS (SELECT 1 FROM dbo.Category WHERE slug = N'dev-audio')
        INSERT dbo.Category (parentCategoryId, name, slug, isActive) VALUES (@ElectronicsId, N'Audio', N'dev-audio', 1);
    SELECT @AudioId = id FROM dbo.Category WHERE slug = N'dev-audio';
    UPDATE dbo.Category SET parentCategoryId = @ElectronicsId, name = N'Audio', isActive = 1 WHERE id = @AudioId;

    IF NOT EXISTS (SELECT 1 FROM dbo.Category WHERE slug = N'dev-collectibles')
        INSERT dbo.Category (name, slug, isActive) VALUES (N'Collectibles', N'dev-collectibles', 1);
    SELECT @CollectiblesId = id FROM dbo.Category WHERE slug = N'dev-collectibles';
    UPDATE dbo.Category SET name = N'Collectibles', isActive = 1 WHERE id = @CollectiblesId;

    IF EXISTS (SELECT 1 FROM dbo.Product WHERE title = N'[DEV-SEED] Smartphone Pro 128GB')
        UPDATE dbo.Product SET description = N'Điện thoại demo cho kiểm thử catalog và giỏ hàng.', categoryId = @PhonesId, brand = N'DevTech', itemCondition = N'NEW', updatedAt = @Now WHERE title = N'[DEV-SEED] Smartphone Pro 128GB';
    ELSE
        INSERT dbo.Product (title, description, categoryId, brand, itemCondition, createdAt, updatedAt) VALUES (N'[DEV-SEED] Smartphone Pro 128GB', N'Điện thoại demo cho kiểm thử catalog và giỏ hàng.', @PhonesId, N'DevTech', N'NEW', @Now, @Now);
    SELECT @ProductPhoneId = id FROM dbo.Product WHERE title = N'[DEV-SEED] Smartphone Pro 128GB';

    IF EXISTS (SELECT 1 FROM dbo.Product WHERE title = N'[DEV-SEED] Wireless Headphones')
        UPDATE dbo.Product SET description = N'Tai nghe không dây demo, chống ồn chủ động.', categoryId = @AudioId, brand = N'DevSound', itemCondition = N'NEW', updatedAt = @Now WHERE title = N'[DEV-SEED] Wireless Headphones';
    ELSE
        INSERT dbo.Product (title, description, categoryId, brand, itemCondition, createdAt, updatedAt) VALUES (N'[DEV-SEED] Wireless Headphones', N'Tai nghe không dây demo, chống ồn chủ động.', @AudioId, N'DevSound', N'NEW', @Now, @Now);
    SELECT @ProductHeadphonesId = id FROM dbo.Product WHERE title = N'[DEV-SEED] Wireless Headphones';

    IF EXISTS (SELECT 1 FROM dbo.Product WHERE title = N'[DEV-SEED] Vintage Camera')
        UPDATE dbo.Product SET description = N'Máy ảnh cổ demo cho listing đấu giá.', categoryId = @CollectiblesId, brand = N'DevCamera', itemCondition = N'USED_GOOD', updatedAt = @Now WHERE title = N'[DEV-SEED] Vintage Camera';
    ELSE
        INSERT dbo.Product (title, description, categoryId, brand, itemCondition, createdAt, updatedAt) VALUES (N'[DEV-SEED] Vintage Camera', N'Máy ảnh cổ demo cho listing đấu giá.', @CollectiblesId, N'DevCamera', N'USED_GOOD', @Now, @Now);
    SELECT @ProductCameraId = id FROM dbo.Product WHERE title = N'[DEV-SEED] Vintage Camera';

    DELETE FROM dbo.ProductImage WHERE productId IN (@ProductPhoneId, @ProductHeadphonesId, @ProductCameraId);
    INSERT dbo.ProductImage (productId, imageUrl, sortOrder, isPrimary) VALUES
        (@ProductPhoneId, N'https://placehold.co/800x600?text=Smartphone+Front', 0, 1),
        (@ProductPhoneId, N'https://placehold.co/800x600?text=Smartphone+Back', 1, 0),
        (@ProductHeadphonesId, N'https://placehold.co/800x600?text=Headphones', 0, 1),
        (@ProductHeadphonesId, N'https://placehold.co/800x600?text=Headphones+Case', 1, 0),
        (@ProductCameraId, N'https://placehold.co/800x600?text=Vintage+Camera', 0, 1),
        (@ProductCameraId, N'https://placehold.co/800x600?text=Camera+Back', 1, 0);

    IF EXISTS (SELECT 1 FROM dbo.Store WHERE sellerId = @SellerId)
        UPDATE dbo.Store SET storeName = N'Dev Demo Store', description = N'Cửa hàng dữ liệu mẫu phục vụ development.', updatedAt = @Now WHERE sellerId = @SellerId;
    ELSE
        INSERT dbo.Store (sellerId, storeName, description, createdAt, updatedAt) VALUES (@SellerId, N'Dev Demo Store', N'Cửa hàng dữ liệu mẫu phục vụ development.', @Now, @Now);

    IF NOT EXISTS (SELECT 1 FROM dbo.ShippingPolicy WHERE sellerId = @SellerId AND name = N'DEV Standard Shipping')
        INSERT dbo.ShippingPolicy (sellerId, name, shippingType, isActive, createdAt) VALUES (@SellerId, N'DEV Standard Shipping', N'FLAT_RATE', 1, @Now);
    SELECT @ShippingPolicyId = id FROM dbo.ShippingPolicy WHERE sellerId = @SellerId AND name = N'DEV Standard Shipping';
    UPDATE dbo.ShippingPolicy SET shippingType = N'FLAT_RATE', isActive = 1 WHERE id = @ShippingPolicyId;

    IF EXISTS (SELECT 1 FROM dbo.ShippingOption WHERE shippingPolicyId = @ShippingPolicyId AND serviceName = N'Standard delivery')
        UPDATE dbo.ShippingOption SET serviceType = N'STANDARD', flatRate = 30000, estimatedMinDays = 2, estimatedMaxDays = 5, isActive = 1 WHERE shippingPolicyId = @ShippingPolicyId AND serviceName = N'Standard delivery';
    ELSE
        INSERT dbo.ShippingOption (shippingPolicyId, serviceName, serviceType, flatRate, estimatedMinDays, estimatedMaxDays, isActive) VALUES (@ShippingPolicyId, N'Standard delivery', N'STANDARD', 30000, 2, 5, 1);

    IF EXISTS (SELECT 1 FROM dbo.ShippingOption WHERE shippingPolicyId = @ShippingPolicyId AND serviceName = N'Express delivery')
        UPDATE dbo.ShippingOption SET serviceType = N'EXPRESS', flatRate = 70000, estimatedMinDays = 1, estimatedMaxDays = 2, isActive = 1 WHERE shippingPolicyId = @ShippingPolicyId AND serviceName = N'Express delivery';
    ELSE
        INSERT dbo.ShippingOption (shippingPolicyId, serviceName, serviceType, flatRate, estimatedMinDays, estimatedMaxDays, isActive) VALUES (@ShippingPolicyId, N'Express delivery', N'EXPRESS', 70000, 1, 2, 1);

    IF EXISTS (SELECT 1 FROM dbo.Listing WHERE productId = @ProductPhoneId AND sellerId = @SellerId AND listingType = N'FIXED_PRICE')
        UPDATE dbo.Listing SET shippingPolicyId = @ShippingPolicyId, price = 12990000, quantity = 10, allowBestOffer = 1, status = N'ACTIVE', publishedAt = DATEADD(day, -1, @Now), endedAt = NULL WHERE productId = @ProductPhoneId AND sellerId = @SellerId AND listingType = N'FIXED_PRICE';
    ELSE
        INSERT dbo.Listing (productId, sellerId, shippingPolicyId, listingType, price, quantity, allowBestOffer, status, createdAt, publishedAt) VALUES (@ProductPhoneId, @SellerId, @ShippingPolicyId, N'FIXED_PRICE', 12990000, 10, 1, N'ACTIVE', @Now, DATEADD(day, -1, @Now));
    SELECT @PhoneListingId = MIN(id) FROM dbo.Listing WHERE productId = @ProductPhoneId AND sellerId = @SellerId AND listingType = N'FIXED_PRICE';

    IF EXISTS (SELECT 1 FROM dbo.Listing WHERE productId = @ProductHeadphonesId AND sellerId = @SellerId AND listingType = N'FIXED_PRICE')
        UPDATE dbo.Listing SET shippingPolicyId = @ShippingPolicyId, price = 2490000, quantity = 20, allowBestOffer = 0, status = N'ACTIVE', publishedAt = DATEADD(day, -1, @Now), endedAt = NULL WHERE productId = @ProductHeadphonesId AND sellerId = @SellerId AND listingType = N'FIXED_PRICE';
    ELSE
        INSERT dbo.Listing (productId, sellerId, shippingPolicyId, listingType, price, quantity, allowBestOffer, status, createdAt, publishedAt) VALUES (@ProductHeadphonesId, @SellerId, @ShippingPolicyId, N'FIXED_PRICE', 2490000, 20, 0, N'ACTIVE', @Now, DATEADD(day, -1, @Now));
    SELECT @HeadphonesListingId = MIN(id) FROM dbo.Listing WHERE productId = @ProductHeadphonesId AND sellerId = @SellerId AND listingType = N'FIXED_PRICE';

    IF EXISTS (SELECT 1 FROM dbo.Listing WHERE productId = @ProductCameraId AND sellerId = @SellerId AND listingType = N'AUCTION')
        UPDATE dbo.Listing SET shippingPolicyId = @ShippingPolicyId, price = NULL, quantity = 1, allowBestOffer = 0, status = N'ACTIVE', publishedAt = DATEADD(day, -1, @Now), endedAt = DATEADD(day, 7, @Now) WHERE productId = @ProductCameraId AND sellerId = @SellerId AND listingType = N'AUCTION';
    ELSE
        INSERT dbo.Listing (productId, sellerId, shippingPolicyId, listingType, price, quantity, allowBestOffer, status, createdAt, publishedAt, endedAt) VALUES (@ProductCameraId, @SellerId, @ShippingPolicyId, N'AUCTION', NULL, 1, 0, N'ACTIVE', @Now, DATEADD(day, -1, @Now), DATEADD(day, 7, @Now));
    SELECT @CameraListingId = MIN(id) FROM dbo.Listing WHERE productId = @ProductCameraId AND sellerId = @SellerId AND listingType = N'AUCTION';

    IF EXISTS (SELECT 1 FROM dbo.Inventory WHERE listingId = @PhoneListingId)
        UPDATE dbo.Inventory SET quantity = 10, reservedQuantity = 0, lastUpdated = @Now WHERE listingId = @PhoneListingId;
    ELSE INSERT dbo.Inventory (listingId, quantity, reservedQuantity, lastUpdated) VALUES (@PhoneListingId, 10, 0, @Now);
    IF EXISTS (SELECT 1 FROM dbo.Inventory WHERE listingId = @HeadphonesListingId)
        UPDATE dbo.Inventory SET quantity = 20, reservedQuantity = 1, lastUpdated = @Now WHERE listingId = @HeadphonesListingId;
    ELSE INSERT dbo.Inventory (listingId, quantity, reservedQuantity, lastUpdated) VALUES (@HeadphonesListingId, 20, 1, @Now);
    IF EXISTS (SELECT 1 FROM dbo.Inventory WHERE listingId = @CameraListingId)
        UPDATE dbo.Inventory SET quantity = 1, reservedQuantity = 0, lastUpdated = @Now WHERE listingId = @CameraListingId;
    ELSE INSERT dbo.Inventory (listingId, quantity, reservedQuantity, lastUpdated) VALUES (@CameraListingId, 1, 0, @Now);

    IF EXISTS (SELECT 1 FROM dbo.Auction WHERE listingId = @CameraListingId)
        UPDATE dbo.Auction SET startingPrice = 1500000, currentPrice = 1850000, bidIncrement = 100000, startTime = DATEADD(day, -1, @Now), endTime = DATEADD(day, 7, @Now), highestBidderId = @BuyerId, winnerId = NULL, winningPrice = NULL, bidCount = 1, status = N'ACTIVE', resolvedAt = NULL WHERE listingId = @CameraListingId;
    ELSE
        INSERT dbo.Auction (listingId, startingPrice, currentPrice, bidIncrement, startTime, endTime, highestBidderId, bidCount, status) VALUES (@CameraListingId, 1500000, 1850000, 100000, DATEADD(day, -1, @Now), DATEADD(day, 7, @Now), @BuyerId, 1, N'ACTIVE');

    IF EXISTS (SELECT 1 FROM dbo.Cart WHERE userId = @BuyerId)
        UPDATE dbo.Cart SET updatedAt = @Now WHERE userId = @BuyerId;
    ELSE INSERT dbo.Cart (userId, createdAt, updatedAt) VALUES (@BuyerId, @Now, @Now);
    SELECT @CartId = id FROM dbo.Cart WHERE userId = @BuyerId;

    IF EXISTS (SELECT 1 FROM dbo.CartItem WHERE cartId = @CartId AND listingId = @PhoneListingId)
        UPDATE dbo.CartItem SET quantity = 1, addedAt = @Now WHERE cartId = @CartId AND listingId = @PhoneListingId;
    ELSE INSERT dbo.CartItem (cartId, listingId, quantity, addedAt) VALUES (@CartId, @PhoneListingId, 1, @Now);
    IF EXISTS (SELECT 1 FROM dbo.CartItem WHERE cartId = @CartId AND listingId = @HeadphonesListingId)
        UPDATE dbo.CartItem SET quantity = 2, addedAt = @Now WHERE cartId = @CartId AND listingId = @HeadphonesListingId;
    ELSE INSERT dbo.CartItem (cartId, listingId, quantity, addedAt) VALUES (@CartId, @HeadphonesListingId, 2, @Now);

    COMMIT;

    SELECT N'buyer.dev@example.test' AS LoginEmail, N'DevPass123!' AS LoginPassword;
    SELECT p.id AS ProductId, p.title, l.id AS ListingId, l.listingType, COALESCE(a.currentPrice, l.price) AS DisplayPrice
    FROM dbo.Product p
    JOIN dbo.Listing l ON l.productId = p.id
    LEFT JOIN dbo.Auction a ON a.listingId = l.id
    WHERE p.title LIKE N'\[DEV-SEED\]%' ESCAPE N'\'
    ORDER BY p.id;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
