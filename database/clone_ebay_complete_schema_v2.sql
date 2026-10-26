/*
    Clone eBay - Extended SQL Server Schema
    Expanded from lecturer-provided minimum schema.
    Target: Microsoft SQL Server
*/

IF DB_ID(N'CloneEbayDB_V2') IS NOT NULL
BEGIN
    ALTER DATABASE CloneEbayDB_V2 SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE CloneEbayDB_V2;
END
GO

CREATE DATABASE CloneEbayDB_V2;
GO
USE CloneEbayDB_V2;
GO

-- 1. USER / AUTH
CREATE TABLE dbo.[User] (
    id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(100) NOT NULL,
    email NVARCHAR(255) NOT NULL UNIQUE,
    passwordHash NVARCHAR(500) NULL,
    role NVARCHAR(20) NOT NULL CONSTRAINT DF_User_Role DEFAULT 'BUYER',
    avatarURL NVARCHAR(1000) NULL,
    phoneNumber NVARCHAR(30) NULL,
    emailVerified BIT NOT NULL CONSTRAINT DF_User_EmailVerified DEFAULT 0,
    phoneVerified BIT NOT NULL CONSTRAINT DF_User_PhoneVerified DEFAULT 0,
    accountStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_User_Status DEFAULT 'ACTIVE',
    createdAt DATETIME2(3) NOT NULL CONSTRAINT DF_User_CreatedAt DEFAULT SYSUTCDATETIME(),
    updatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_User_UpdatedAt DEFAULT SYSUTCDATETIME(),
    lastLoginAt DATETIME2(3) NULL,
    CONSTRAINT CK_User_Role CHECK (role IN ('BUYER','SELLER','ADMIN')),
    CONSTRAINT CK_User_Status CHECK (accountStatus IN ('ACTIVE','LOCKED','SUSPENDED','DELETED'))
);
GO
CREATE INDEX IX_User_Username ON dbo.[User](username);
CREATE INDEX IX_User_PhoneNumber ON dbo.[User](phoneNumber) WHERE phoneNumber IS NOT NULL;
GO

CREATE TABLE dbo.ExternalLogin (
    id INT IDENTITY(1,1) PRIMARY KEY,
    userId INT NOT NULL,
    provider NVARCHAR(50) NOT NULL,
    providerUserId NVARCHAR(255) NOT NULL,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ExternalLogin_User FOREIGN KEY (userId) REFERENCES dbo.[User](id),
    CONSTRAINT UQ_ExternalLogin_ProviderUser UNIQUE (provider, providerUserId)
);
GO

CREATE TABLE dbo.VerificationToken (
    id INT IDENTITY(1,1) PRIMARY KEY,
    userId INT NOT NULL,
    tokenType NVARCHAR(30) NOT NULL,
    tokenHash NVARCHAR(500) NOT NULL,
    expiresAt DATETIME2(3) NOT NULL,
    usedAt DATETIME2(3) NULL,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_VerificationToken_User FOREIGN KEY (userId) REFERENCES dbo.[User](id),
    CONSTRAINT CK_VerificationToken_Type CHECK (tokenType IN ('EMAIL_VERIFY','PASSWORD_RESET'))
);
GO
CREATE INDEX IX_VerificationToken_UserType ON dbo.VerificationToken(userId, tokenType, expiresAt);
GO

CREATE TABLE dbo.OtpChallenge (
    id INT IDENTITY(1,1) PRIMARY KEY,
    userId INT NULL,
    phoneNumber NVARCHAR(30) NOT NULL,
    purpose NVARCHAR(30) NOT NULL,
    otpHash NVARCHAR(500) NOT NULL,
    expiresAt DATETIME2(3) NOT NULL,
    attemptCount INT NOT NULL DEFAULT 0,
    resendCount INT NOT NULL DEFAULT 0,
    verifiedAt DATETIME2(3) NULL,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_OtpChallenge_User FOREIGN KEY (userId) REFERENCES dbo.[User](id),
    CONSTRAINT CK_OtpChallenge_Purpose CHECK (purpose IN ('REGISTER','LOGIN_2FA','PASSWORD_RESET','PHONE_CHANGE')),
    CONSTRAINT CK_OtpChallenge_Attempt CHECK (attemptCount >= 0),
    CONSTRAINT CK_OtpChallenge_Resend CHECK (resendCount >= 0)
);
GO

-- 2. ADDRESS
CREATE TABLE dbo.Address (
    id INT IDENTITY(1,1) PRIMARY KEY,
    userId INT NOT NULL,
    fullName NVARCHAR(150) NOT NULL,
    phone NVARCHAR(30) NOT NULL,
    street NVARCHAR(255) NOT NULL,
    addressLine2 NVARCHAR(255) NULL,
    city NVARCHAR(100) NOT NULL,
    state NVARCHAR(100) NULL,
    postalCode NVARCHAR(30) NULL,
    country NVARCHAR(100) NOT NULL,
    countryCode NVARCHAR(10) NULL,
    isDefault BIT NOT NULL DEFAULT 0,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    updatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Address_User FOREIGN KEY (userId) REFERENCES dbo.[User](id)
);
GO
CREATE INDEX IX_Address_User ON dbo.Address(userId);
CREATE UNIQUE INDEX UX_Address_OneDefaultPerUser ON dbo.Address(userId) WHERE isDefault = 1;
GO

-- 3. CATEGORY / PRODUCT
CREATE TABLE dbo.Category (
    id INT IDENTITY(1,1) PRIMARY KEY,
    parentCategoryId INT NULL,
    name NVARCHAR(150) NOT NULL,
    slug NVARCHAR(180) NOT NULL UNIQUE,
    isActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Category_Parent FOREIGN KEY (parentCategoryId) REFERENCES dbo.Category(id)
);
GO

CREATE TABLE dbo.Product (
    id INT IDENTITY(1,1) PRIMARY KEY,
    title NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX) NULL,
    categoryId INT NOT NULL,
    brand NVARCHAR(150) NULL,
    itemCondition NVARCHAR(50) NULL,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    updatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Product_Category FOREIGN KEY (categoryId) REFERENCES dbo.Category(id)
);
GO
CREATE INDEX IX_Product_Category ON dbo.Product(categoryId);
CREATE INDEX IX_Product_Title ON dbo.Product(title);
GO

CREATE TABLE dbo.ProductImage (
    id INT IDENTITY(1,1) PRIMARY KEY,
    productId INT NOT NULL,
    imageUrl NVARCHAR(1000) NOT NULL,
    sortOrder INT NOT NULL DEFAULT 0,
    isPrimary BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ProductImage_Product FOREIGN KEY (productId) REFERENCES dbo.Product(id) ON DELETE CASCADE,
    CONSTRAINT CK_ProductImage_Order CHECK (sortOrder >= 0)
);
GO

-- 4. SHIPPING CONFIG
CREATE TABLE dbo.ShippingPolicy (
    id INT IDENTITY(1,1) PRIMARY KEY,
    sellerId INT NOT NULL,
    name NVARCHAR(150) NOT NULL,
    shippingType NVARCHAR(20) NOT NULL,
    isActive BIT NOT NULL DEFAULT 1,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ShippingPolicy_Seller FOREIGN KEY (sellerId) REFERENCES dbo.[User](id),
    CONSTRAINT CK_ShippingPolicy_Type CHECK (shippingType IN ('FREE','FLAT_RATE','CALCULATED'))
);
GO

CREATE TABLE dbo.ShippingOption (
    id INT IDENTITY(1,1) PRIMARY KEY,
    shippingPolicyId INT NOT NULL,
    serviceName NVARCHAR(150) NOT NULL,
    serviceType NVARCHAR(20) NOT NULL,
    flatRate DECIMAL(18,2) NULL,
    estimatedMinDays INT NULL,
    estimatedMaxDays INT NULL,
    isActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_ShippingOption_Policy FOREIGN KEY (shippingPolicyId) REFERENCES dbo.ShippingPolicy(id) ON DELETE CASCADE,
    CONSTRAINT CK_ShippingOption_ServiceType CHECK (serviceType IN ('STANDARD','EXPRESS','LOCAL_PICKUP')),
    CONSTRAINT CK_ShippingOption_FlatRate CHECK (flatRate IS NULL OR flatRate >= 0),
    CONSTRAINT CK_ShippingOption_Days CHECK ((estimatedMinDays IS NULL OR estimatedMinDays >= 0) AND (estimatedMaxDays IS NULL OR estimatedMaxDays >= 0) AND (estimatedMinDays IS NULL OR estimatedMaxDays IS NULL OR estimatedMinDays <= estimatedMaxDays))
);
GO

CREATE TABLE dbo.ShippingRegion (
    id INT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(50) NOT NULL UNIQUE,
    name NVARCHAR(150) NOT NULL,
    regionType NVARCHAR(20) NOT NULL,
    surcharge DECIMAL(18,2) NOT NULL DEFAULT 0,
    isActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT CK_ShippingRegion_Type CHECK (regionType IN ('LOCAL','DOMESTIC','REMOTE','INTERNATIONAL')),
    CONSTRAINT CK_ShippingRegion_Surcharge CHECK (surcharge >= 0)
);
GO

-- 5. TAX CONFIG
CREATE TABLE dbo.TaxRegion (
    id INT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(50) NOT NULL UNIQUE,
    name NVARCHAR(150) NOT NULL,
    taxRate DECIMAL(7,4) NOT NULL,
    taxShipping BIT NOT NULL DEFAULT 0,
    isActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT CK_TaxRegion_Rate CHECK (taxRate >= 0 AND taxRate <= 100)
);
GO

-- 6. LISTING / PACKAGE / INVENTORY
CREATE TABLE dbo.Listing (
    id INT IDENTITY(1,1) PRIMARY KEY,
    productId INT NOT NULL,
    sellerId INT NOT NULL,
    shippingPolicyId INT NULL,
    listingType NVARCHAR(20) NOT NULL,
    price DECIMAL(18,2) NULL,
    quantity INT NOT NULL DEFAULT 1,
    allowBestOffer BIT NOT NULL DEFAULT 0,
    status NVARCHAR(20) NOT NULL DEFAULT 'DRAFT',
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    publishedAt DATETIME2(3) NULL,
    endedAt DATETIME2(3) NULL,
    CONSTRAINT FK_Listing_Product FOREIGN KEY (productId) REFERENCES dbo.Product(id),
    CONSTRAINT FK_Listing_Seller FOREIGN KEY (sellerId) REFERENCES dbo.[User](id),
    CONSTRAINT FK_Listing_ShippingPolicy FOREIGN KEY (shippingPolicyId) REFERENCES dbo.ShippingPolicy(id),
    CONSTRAINT CK_Listing_Type CHECK (listingType IN ('FIXED_PRICE','AUCTION')),
    CONSTRAINT CK_Listing_Price CHECK (price IS NULL OR price >= 0),
    CONSTRAINT CK_Listing_Quantity CHECK (quantity >= 0),
    CONSTRAINT CK_Listing_Status CHECK (status IN ('DRAFT','ACTIVE','PAUSED','ENDED','SOLD','CANCELLED'))
);
GO
CREATE INDEX IX_Listing_Product ON dbo.Listing(productId);
CREATE INDEX IX_Listing_SellerStatus ON dbo.Listing(sellerId, status);
CREATE INDEX IX_Listing_TypeStatus ON dbo.Listing(listingType, status);
GO

CREATE TABLE dbo.PackageInfo (
    id INT IDENTITY(1,1) PRIMARY KEY,
    listingId INT NOT NULL UNIQUE,
    weightKg DECIMAL(10,3) NOT NULL,
    lengthCm DECIMAL(10,2) NULL,
    widthCm DECIMAL(10,2) NULL,
    heightCm DECIMAL(10,2) NULL,
    CONSTRAINT FK_PackageInfo_Listing FOREIGN KEY (listingId) REFERENCES dbo.Listing(id) ON DELETE CASCADE,
    CONSTRAINT CK_PackageInfo_Weight CHECK (weightKg > 0),
    CONSTRAINT CK_PackageInfo_Dimensions CHECK ((lengthCm IS NULL OR lengthCm > 0) AND (widthCm IS NULL OR widthCm > 0) AND (heightCm IS NULL OR heightCm > 0))
);
GO

CREATE TABLE dbo.Inventory (
    id INT IDENTITY(1,1) PRIMARY KEY,
    listingId INT NOT NULL UNIQUE,
    quantity INT NOT NULL,
    reservedQuantity INT NOT NULL DEFAULT 0,
    lastUpdated DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    rowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_Inventory_Listing FOREIGN KEY (listingId) REFERENCES dbo.Listing(id),
    CONSTRAINT CK_Inventory_Quantity CHECK (quantity >= 0),
    CONSTRAINT CK_Inventory_Reserved CHECK (reservedQuantity >= 0 AND reservedQuantity <= quantity)
);
GO

-- 7. CART
CREATE TABLE dbo.Cart (
    id INT IDENTITY(1,1) PRIMARY KEY,
    userId INT NOT NULL UNIQUE,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    updatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Cart_User FOREIGN KEY (userId) REFERENCES dbo.[User](id)
);
GO

CREATE TABLE dbo.CartItem (
    id INT IDENTITY(1,1) PRIMARY KEY,
    cartId INT NOT NULL,
    listingId INT NOT NULL,
    quantity INT NOT NULL,
    addedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CartItem_Cart FOREIGN KEY (cartId) REFERENCES dbo.Cart(id) ON DELETE CASCADE,
    CONSTRAINT FK_CartItem_Listing FOREIGN KEY (listingId) REFERENCES dbo.Listing(id),
    CONSTRAINT UQ_CartItem UNIQUE (cartId, listingId),
    CONSTRAINT CK_CartItem_Quantity CHECK (quantity > 0)
);
GO

-- 8. AUCTION / BID
CREATE TABLE dbo.Auction (
    id INT IDENTITY(1,1) PRIMARY KEY,
    listingId INT NOT NULL UNIQUE,
    startingPrice DECIMAL(18,2) NOT NULL,
    currentPrice DECIMAL(18,2) NOT NULL,
    bidIncrement DECIMAL(18,2) NOT NULL,
    startTime DATETIME2(3) NOT NULL,
    endTime DATETIME2(3) NOT NULL,
    highestBidderId INT NULL,
    winnerId INT NULL,
    winningPrice DECIMAL(18,2) NULL,
    bidCount INT NOT NULL DEFAULT 0,
    status NVARCHAR(20) NOT NULL DEFAULT 'SCHEDULED',
    resolvedAt DATETIME2(3) NULL,
    rowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_Auction_Listing FOREIGN KEY (listingId) REFERENCES dbo.Listing(id),
    CONSTRAINT FK_Auction_HighestBidder FOREIGN KEY (highestBidderId) REFERENCES dbo.[User](id),
    CONSTRAINT FK_Auction_Winner FOREIGN KEY (winnerId) REFERENCES dbo.[User](id),
    CONSTRAINT CK_Auction_Prices CHECK (startingPrice >= 0 AND currentPrice >= 0 AND bidIncrement > 0 AND (winningPrice IS NULL OR winningPrice >= 0)),
    CONSTRAINT CK_Auction_Time CHECK (startTime < endTime),
    CONSTRAINT CK_Auction_BidCount CHECK (bidCount >= 0),
    CONSTRAINT CK_Auction_Status CHECK (status IN ('SCHEDULED','ACTIVE','ENDED','CANCELLED'))
);
GO
CREATE INDEX IX_Auction_StatusEndTime ON dbo.Auction(status, endTime);
GO

CREATE TABLE dbo.Bid (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    auctionId INT NOT NULL,
    bidderId INT NOT NULL,
    maximumBid DECIMAL(18,2) NOT NULL,
    effectiveBid DECIMAL(18,2) NOT NULL,
    status NVARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Bid_Auction FOREIGN KEY (auctionId) REFERENCES dbo.Auction(id),
    CONSTRAINT FK_Bid_Bidder FOREIGN KEY (bidderId) REFERENCES dbo.[User](id),
    CONSTRAINT CK_Bid_Amounts CHECK (maximumBid > 0 AND effectiveBid >= 0 AND effectiveBid <= maximumBid),
    CONSTRAINT CK_Bid_Status CHECK (status IN ('ACTIVE','OUTBID','WINNING','WON','RETRACTED','CANCELLED'))
);
GO
CREATE INDEX IX_Bid_AuctionCreatedAt ON dbo.Bid(auctionId, createdAt, id);
CREATE INDEX IX_Bid_AuctionMaximum ON dbo.Bid(auctionId, maximumBid DESC, createdAt ASC);
CREATE INDEX IX_Bid_Bidder ON dbo.Bid(bidderId, createdAt DESC);
GO

-- 9. COUPON
CREATE TABLE dbo.Coupon (
    id INT IDENTITY(1,1) PRIMARY KEY,
    sellerId INT NULL,
    code NVARCHAR(50) NOT NULL UNIQUE,
    discountType NVARCHAR(20) NOT NULL,
    discountValue DECIMAL(18,2) NOT NULL,
    minOrderValue DECIMAL(18,2) NULL,
    maxDiscountAmount DECIMAL(18,2) NULL,
    startDate DATETIME2(3) NOT NULL,
    endDate DATETIME2(3) NOT NULL,
    maxUsage INT NULL,
    usageCount INT NOT NULL DEFAULT 0,
    perUserLimit INT NULL,
    isActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Coupon_Seller FOREIGN KEY (sellerId) REFERENCES dbo.[User](id),
    CONSTRAINT CK_Coupon_Type CHECK (discountType IN ('PERCENT','FIXED_AMOUNT')),
    CONSTRAINT CK_Coupon_Value CHECK (discountValue > 0),
    CONSTRAINT CK_Coupon_Date CHECK (startDate < endDate),
    CONSTRAINT CK_Coupon_MaxUsage CHECK (maxUsage IS NULL OR maxUsage > 0),
    CONSTRAINT CK_Coupon_UsageCount CHECK (usageCount >= 0),
    CONSTRAINT CK_Coupon_PerUser CHECK (perUserLimit IS NULL OR perUserLimit > 0)
);
GO

CREATE TABLE dbo.CouponListing (
    couponId INT NOT NULL,
    listingId INT NOT NULL,
    CONSTRAINT PK_CouponListing PRIMARY KEY (couponId, listingId),
    CONSTRAINT FK_CouponListing_Coupon FOREIGN KEY (couponId) REFERENCES dbo.Coupon(id) ON DELETE CASCADE,
    CONSTRAINT FK_CouponListing_Listing FOREIGN KEY (listingId) REFERENCES dbo.Listing(id) ON DELETE CASCADE
);
GO

-- 10. CHECKOUT / ORDER
CREATE TABLE dbo.CheckoutGroup (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    buyerId INT NULL,
    guestEmail NVARCHAR(255) NULL,
    subtotal DECIMAL(18,2) NOT NULL,
    discountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    shippingFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    taxAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    importCharge DECIMAL(18,2) NOT NULL DEFAULT 0,
    totalAmount DECIMAL(18,2) NOT NULL,
    currency NVARCHAR(10) NOT NULL DEFAULT 'VND',
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CheckoutGroup_Buyer FOREIGN KEY (buyerId) REFERENCES dbo.[User](id),
    CONSTRAINT CK_CheckoutGroup_BuyerOrGuest CHECK (buyerId IS NOT NULL OR guestEmail IS NOT NULL),
    CONSTRAINT CK_CheckoutGroup_Money CHECK (subtotal >= 0 AND discountAmount >= 0 AND shippingFee >= 0 AND taxAmount >= 0 AND importCharge >= 0 AND totalAmount >= 0)
);
GO

CREATE TABLE dbo.OrderTable (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    checkoutGroupId BIGINT NULL,
    buyerId INT NULL,
    guestEmail NVARCHAR(255) NULL,
    sellerId INT NOT NULL,
    addressId INT NULL,
    orderDate DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    paymentDeadline DATETIME2(3) NULL,
    shippingFullName NVARCHAR(150) NOT NULL,
    shippingPhone NVARCHAR(30) NOT NULL,
    shippingStreet NVARCHAR(255) NOT NULL,
    shippingAddressLine2 NVARCHAR(255) NULL,
    shippingCity NVARCHAR(100) NOT NULL,
    shippingState NVARCHAR(100) NULL,
    shippingPostalCode NVARCHAR(30) NULL,
    shippingCountry NVARCHAR(100) NOT NULL,
    shippingCountryCode NVARCHAR(10) NULL,
    shippingOptionId INT NULL,
    shippingServiceName NVARCHAR(150) NULL,
    shippingServiceType NVARCHAR(20) NULL,
    subtotal DECIMAL(18,2) NOT NULL,
    discountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    shippingFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    taxRateSnapshot DECIMAL(7,4) NULL,
    taxAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    importCharge DECIMAL(18,2) NOT NULL DEFAULT 0,
    totalAmount DECIMAL(18,2) NOT NULL,
    currency NVARCHAR(10) NOT NULL DEFAULT 'VND',
    couponId INT NULL,
    status NVARCHAR(30) NOT NULL DEFAULT 'PENDING_PAYMENT',
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    updatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Order_CheckoutGroup FOREIGN KEY (checkoutGroupId) REFERENCES dbo.CheckoutGroup(id),
    CONSTRAINT FK_Order_Buyer FOREIGN KEY (buyerId) REFERENCES dbo.[User](id),
    CONSTRAINT FK_Order_Seller FOREIGN KEY (sellerId) REFERENCES dbo.[User](id),
    CONSTRAINT FK_Order_Address FOREIGN KEY (addressId) REFERENCES dbo.Address(id),
    CONSTRAINT FK_Order_ShippingOption FOREIGN KEY (shippingOptionId) REFERENCES dbo.ShippingOption(id),
    CONSTRAINT FK_Order_Coupon FOREIGN KEY (couponId) REFERENCES dbo.Coupon(id),
    CONSTRAINT CK_Order_BuyerOrGuest CHECK (buyerId IS NOT NULL OR guestEmail IS NOT NULL),
    CONSTRAINT CK_Order_Money CHECK (subtotal >= 0 AND discountAmount >= 0 AND shippingFee >= 0 AND taxAmount >= 0 AND importCharge >= 0 AND totalAmount >= 0),
    CONSTRAINT CK_Order_Status CHECK (status IN ('PENDING_PAYMENT','AWAITING_SHIPMENT','SHIPPED','DELIVERED','CANCEL_REQUESTED','CANCELLED','RETURN_REQUESTED','RETURNED'))
);
GO
CREATE INDEX IX_Order_BuyerCreatedAt ON dbo.OrderTable(buyerId, createdAt DESC);
CREATE INDEX IX_Order_SellerStatus ON dbo.OrderTable(sellerId, status, createdAt DESC);
CREATE INDEX IX_Order_CheckoutGroup ON dbo.OrderTable(checkoutGroupId);
GO

CREATE TABLE dbo.OrderItem (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    orderId BIGINT NOT NULL,
    listingId INT NOT NULL,
    productId INT NOT NULL,
    sellerId INT NOT NULL,
    quantity INT NOT NULL,
    unitPrice DECIMAL(18,2) NOT NULL,
    productTitle NVARCHAR(255) NOT NULL,
    imageUrl NVARCHAR(1000) NULL,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_OrderItem_Order FOREIGN KEY (orderId) REFERENCES dbo.OrderTable(id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItem_Listing FOREIGN KEY (listingId) REFERENCES dbo.Listing(id),
    CONSTRAINT FK_OrderItem_Product FOREIGN KEY (productId) REFERENCES dbo.Product(id),
    CONSTRAINT FK_OrderItem_Seller FOREIGN KEY (sellerId) REFERENCES dbo.[User](id),
    CONSTRAINT CK_OrderItem_Quantity CHECK (quantity > 0),
    CONSTRAINT CK_OrderItem_UnitPrice CHECK (unitPrice >= 0)
);
GO
CREATE INDEX IX_OrderItem_Order ON dbo.OrderItem(orderId);
GO

CREATE TABLE dbo.CouponUsage (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    couponId INT NOT NULL,
    userId INT NULL,
    orderId BIGINT NOT NULL,
    discountAmount DECIMAL(18,2) NOT NULL,
    usedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CouponUsage_Coupon FOREIGN KEY (couponId) REFERENCES dbo.Coupon(id),
    CONSTRAINT FK_CouponUsage_User FOREIGN KEY (userId) REFERENCES dbo.[User](id),
    CONSTRAINT FK_CouponUsage_Order FOREIGN KEY (orderId) REFERENCES dbo.OrderTable(id),
    CONSTRAINT CK_CouponUsage_Discount CHECK (discountAmount >= 0)
);
GO
CREATE INDEX IX_CouponUsage_CouponUser ON dbo.CouponUsage(couponId, userId);
GO

-- 11. PAYMENT
CREATE TABLE dbo.Payment (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    orderId BIGINT NOT NULL,
    amount DECIMAL(18,2) NOT NULL,
    method NVARCHAR(30) NOT NULL,
    provider NVARCHAR(50) NULL,
    transactionId NVARCHAR(150) NULL,
    status NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    failureReason NVARCHAR(500) NULL,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    paidAt DATETIME2(3) NULL,
    refundedAt DATETIME2(3) NULL,
    CONSTRAINT FK_Payment_Order FOREIGN KEY (orderId) REFERENCES dbo.OrderTable(id),
    CONSTRAINT CK_Payment_Amount CHECK (amount >= 0),
    CONSTRAINT CK_Payment_Method CHECK (method IN ('PAYPAL','COD','CARD','OTHER')),
    CONSTRAINT CK_Payment_Status CHECK (status IN ('PENDING','PROCESSING','PAID','FAILED','REFUNDED'))
);
GO
CREATE INDEX IX_Payment_OrderCreatedAt ON dbo.Payment(orderId, createdAt DESC);
CREATE INDEX IX_Payment_Status ON dbo.Payment(status);
CREATE UNIQUE INDEX UX_Payment_TransactionId ON dbo.Payment(transactionId) WHERE transactionId IS NOT NULL;
GO

-- 12. SHIPPING / TRACKING
CREATE TABLE dbo.ShippingInfo (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    orderId BIGINT NOT NULL UNIQUE,
    carrier NVARCHAR(100) NULL,
    trackingNumber NVARCHAR(150) NULL,
    status NVARCHAR(30) NOT NULL DEFAULT 'PENDING',
    shippedAt DATETIME2(3) NULL,
    estimatedArrival DATETIME2(3) NULL,
    deliveredAt DATETIME2(3) NULL,
    updatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ShippingInfo_Order FOREIGN KEY (orderId) REFERENCES dbo.OrderTable(id),
    CONSTRAINT CK_ShippingInfo_Status CHECK (status IN ('PENDING','LABEL_CREATED','IN_TRANSIT','DELIVERED','FAILED','RETURNED'))
);
GO
CREATE INDEX IX_ShippingInfo_Tracking ON dbo.ShippingInfo(trackingNumber) WHERE trackingNumber IS NOT NULL;
GO

-- 13. RETURN
CREATE TABLE dbo.ReturnRequest (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    orderId BIGINT NOT NULL,
    requestedBy INT NOT NULL,
    reasonCode NVARCHAR(50) NOT NULL,
    reasonDetail NVARCHAR(MAX) NULL,
    status NVARCHAR(30) NOT NULL DEFAULT 'REQUESTED',
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    approvedAt DATETIME2(3) NULL,
    completedAt DATETIME2(3) NULL,
    CONSTRAINT FK_ReturnRequest_Order FOREIGN KEY (orderId) REFERENCES dbo.OrderTable(id),
    CONSTRAINT FK_ReturnRequest_User FOREIGN KEY (requestedBy) REFERENCES dbo.[User](id),
    CONSTRAINT CK_ReturnRequest_Status CHECK (status IN ('REQUESTED','APPROVED','REJECTED','RETURNING','COMPLETED','CANCELLED'))
);
GO

CREATE TABLE dbo.ReturnRequestItem (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    returnRequestId BIGINT NOT NULL,
    orderItemId BIGINT NOT NULL,
    quantity INT NOT NULL,
    CONSTRAINT FK_ReturnRequestItem_Request FOREIGN KEY (returnRequestId) REFERENCES dbo.ReturnRequest(id) ON DELETE CASCADE,
    CONSTRAINT FK_ReturnRequestItem_OrderItem FOREIGN KEY (orderItemId) REFERENCES dbo.OrderItem(id),
    CONSTRAINT UQ_ReturnRequestItem UNIQUE (returnRequestId, orderItemId),
    CONSTRAINT CK_ReturnRequestItem_Quantity CHECK (quantity > 0)
);
GO

-- 14. REVIEW / SELLER FEEDBACK
CREATE TABLE dbo.Review (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    orderItemId BIGINT NOT NULL,
    productId INT NOT NULL,
    reviewerId INT NOT NULL,
    rating INT NOT NULL,
    comment NVARCHAR(MAX) NULL,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Review_OrderItem FOREIGN KEY (orderItemId) REFERENCES dbo.OrderItem(id),
    CONSTRAINT FK_Review_Product FOREIGN KEY (productId) REFERENCES dbo.Product(id),
    CONSTRAINT FK_Review_User FOREIGN KEY (reviewerId) REFERENCES dbo.[User](id),
    CONSTRAINT UQ_Review_OrderItemReviewer UNIQUE (orderItemId, reviewerId),
    CONSTRAINT CK_Review_Rating CHECK (rating BETWEEN 1 AND 5)
);
GO
CREATE INDEX IX_Review_Product ON dbo.Review(productId, createdAt DESC);
GO

CREATE TABLE dbo.SellerFeedback (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    orderId BIGINT NOT NULL,
    sellerId INT NOT NULL,
    buyerId INT NOT NULL,
    feedbackType NVARCHAR(20) NOT NULL,
    comment NVARCHAR(MAX) NULL,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_SellerFeedback_Order FOREIGN KEY (orderId) REFERENCES dbo.OrderTable(id),
    CONSTRAINT FK_SellerFeedback_Seller FOREIGN KEY (sellerId) REFERENCES dbo.[User](id),
    CONSTRAINT FK_SellerFeedback_Buyer FOREIGN KEY (buyerId) REFERENCES dbo.[User](id),
    CONSTRAINT UQ_SellerFeedback_OrderBuyer UNIQUE (orderId, buyerId),
    CONSTRAINT CK_SellerFeedback_Type CHECK (feedbackType IN ('POSITIVE','NEUTRAL','NEGATIVE'))
);
GO

CREATE TABLE dbo.SellerFeedbackSummary (
    sellerId INT PRIMARY KEY,
    averageRating DECIMAL(5,2) NULL,
    totalReviews INT NOT NULL DEFAULT 0,
    positiveRate DECIMAL(7,4) NULL,
    updatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_SellerFeedbackSummary_Seller FOREIGN KEY (sellerId) REFERENCES dbo.[User](id),
    CONSTRAINT CK_SellerFeedbackSummary_Total CHECK (totalReviews >= 0),
    CONSTRAINT CK_SellerFeedbackSummary_Avg CHECK (averageRating IS NULL OR (averageRating >= 0 AND averageRating <= 5)),
    CONSTRAINT CK_SellerFeedbackSummary_Positive CHECK (positiveRate IS NULL OR (positiveRate >= 0 AND positiveRate <= 100))
);
GO

-- 15. WATCHLIST / NOTIFICATION
CREATE TABLE dbo.Watchlist (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    userId INT NOT NULL,
    listingId INT NOT NULL,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Watchlist_User FOREIGN KEY (userId) REFERENCES dbo.[User](id),
    CONSTRAINT FK_Watchlist_Listing FOREIGN KEY (listingId) REFERENCES dbo.Listing(id),
    CONSTRAINT UQ_Watchlist_UserListing UNIQUE (userId, listingId)
);
GO

CREATE TABLE dbo.Notification (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    userId INT NOT NULL,
    type NVARCHAR(40) NOT NULL,
    title NVARCHAR(255) NOT NULL,
    content NVARCHAR(MAX) NULL,
    referenceType NVARCHAR(50) NULL,
    referenceId NVARCHAR(100) NULL,
    isRead BIT NOT NULL DEFAULT 0,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    readAt DATETIME2(3) NULL,
    CONSTRAINT FK_Notification_User FOREIGN KEY (userId) REFERENCES dbo.[User](id)
);
GO
CREATE INDEX IX_Notification_UserUnread ON dbo.Notification(userId, isRead, createdAt DESC);
GO

-- 16. STORE / MESSAGE / DISPUTE
CREATE TABLE dbo.Store (
    id INT IDENTITY(1,1) PRIMARY KEY,
    sellerId INT NOT NULL UNIQUE,
    storeName NVARCHAR(150) NOT NULL,
    description NVARCHAR(MAX) NULL,
    bannerImageURL NVARCHAR(1000) NULL,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    updatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Store_Seller FOREIGN KEY (sellerId) REFERENCES dbo.[User](id)
);
GO

CREATE TABLE dbo.Message (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    senderId INT NOT NULL,
    receiverId INT NOT NULL,
    content NVARCHAR(MAX) NOT NULL,
    sentAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    isRead BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Message_Sender FOREIGN KEY (senderId) REFERENCES dbo.[User](id),
    CONSTRAINT FK_Message_Receiver FOREIGN KEY (receiverId) REFERENCES dbo.[User](id),
    CONSTRAINT CK_Message_DifferentUsers CHECK (senderId <> receiverId)
);
GO
CREATE INDEX IX_Message_Conversation ON dbo.Message(senderId, receiverId, sentAt);
GO

CREATE TABLE dbo.Dispute (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    orderId BIGINT NOT NULL,
    raisedBy INT NOT NULL,
    description NVARCHAR(MAX) NOT NULL,
    status NVARCHAR(30) NOT NULL DEFAULT 'OPEN',
    resolution NVARCHAR(MAX) NULL,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    resolvedAt DATETIME2(3) NULL,
    CONSTRAINT FK_Dispute_Order FOREIGN KEY (orderId) REFERENCES dbo.OrderTable(id),
    CONSTRAINT FK_Dispute_User FOREIGN KEY (raisedBy) REFERENCES dbo.[User](id),
    CONSTRAINT CK_Dispute_Status CHECK (status IN ('OPEN','UNDER_REVIEW','RESOLVED','REJECTED','CLOSED'))
);
GO

-- 17. OPTIONAL GIFT CARD (P2)
CREATE TABLE dbo.GiftCard (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    codeHash NVARCHAR(500) NOT NULL UNIQUE,
    initialBalance DECIMAL(18,2) NOT NULL,
    currentBalance DECIMAL(18,2) NOT NULL,
    linkedUserId INT NULL,
    status NVARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    expiresAt DATETIME2(3) NULL,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_GiftCard_User FOREIGN KEY (linkedUserId) REFERENCES dbo.[User](id),
    CONSTRAINT CK_GiftCard_Balance CHECK (initialBalance >= 0 AND currentBalance >= 0 AND currentBalance <= initialBalance),
    CONSTRAINT CK_GiftCard_Status CHECK (status IN ('ACTIVE','DEPLETED','EXPIRED','BLOCKED'))
);
GO

CREATE TABLE dbo.GiftCardTransaction (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    giftCardId BIGINT NOT NULL,
    orderId BIGINT NULL,
    transactionType NVARCHAR(20) NOT NULL,
    amount DECIMAL(18,2) NOT NULL,
    createdAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_GiftCardTransaction_Card FOREIGN KEY (giftCardId) REFERENCES dbo.GiftCard(id),
    CONSTRAINT FK_GiftCardTransaction_Order FOREIGN KEY (orderId) REFERENCES dbo.OrderTable(id),
    CONSTRAINT CK_GiftCardTransaction_Type CHECK (transactionType IN ('REDEEM','REFUND','ADJUSTMENT')),
    CONSTRAINT CK_GiftCardTransaction_Amount CHECK (amount > 0)
);
GO

-- 18. SEED PROJECT SHIPPING/TAX RULES
INSERT INTO dbo.ShippingRegion(code, name, regionType, surcharge)
VALUES
('LOCAL', N'Local', 'LOCAL', 0),
('DOMESTIC', N'Domestic', 'DOMESTIC', 20000),
('REMOTE', N'Remote', 'REMOTE', 40000),
('INTERNATIONAL', N'International', 'INTERNATIONAL', 100000);
GO

INSERT INTO dbo.TaxRegion(code, name, taxRate, taxShipping)
VALUES
('REGION_A', N'Region A', 0.0000, 0),
('REGION_B', N'Region B', 5.0000, 0),
('REGION_C', N'Region C', 8.0000, 0),
('REGION_D', N'Region D', 10.0000, 0);
GO

-- 19. VIEW
CREATE VIEW dbo.vw_ActiveListings AS
SELECT
    l.id AS listingId,
    p.id AS productId,
    p.title,
    p.description,
    p.categoryId,
    l.sellerId,
    l.listingType,
    l.price,
    l.quantity,
    l.status,
    l.publishedAt,
    a.id AS auctionId,
    a.currentPrice,
    a.startTime,
    a.endTime,
    a.bidCount,
    a.highestBidderId
FROM dbo.Listing l
INNER JOIN dbo.Product p ON p.id = l.productId
LEFT JOIN dbo.Auction a ON a.listingId = l.id
WHERE l.status = 'ACTIVE';
GO

/*
APPLICATION RULES
1) Guest cart stays local/session. Cart tables are authenticated server cart.
2) One CheckoutGroup may create many OrderTable rows; each order belongs to one seller.
3) Server recalculates money; never trust final total from client.
4) Shipping project formula:
      baseFee + weightFee + regionFee + serviceSurcharge
   Keep constants in config/calculator, not controllers.
5) TaxRegion values are project simulation, not official eBay tax rates.
6) Import P0 can use calculator returning 0; importCharge column is kept for extension.
7) Auction PlaceBid must use transaction + Auction.rowVersion concurrency protection.
8) Broadcast SignalR only after DB commit.
9) Review allowed only after delivered order (enforce in application layer).
10) COD starts Payment=PENDING, not PAID.
11) Store timestamps in UTC.
*/
GO
