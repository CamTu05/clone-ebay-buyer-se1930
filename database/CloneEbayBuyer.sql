/*
  Clone eBay - Buyer Module - Database V3 (SQL Server 2019+)
  Fresh CREATE script. Database name: CloneEbayBuyer.
  IMPORTANT: This script does not drop any existing database.
  This script intentionally aborts if CloneEbayBuyer already exists.

  Scope: 33 tables, 1 listing view; account, catalog/listings, shopping cart,
  auctions, multi-seller checkout, group-level mock payment, guest access,
  shipping, cancellation/return requests, feedback, notifications.

  Application rules NOT enforced by foreign keys alone: see bottom of file.
*/
SET NOCOUNT ON;
GO
IF DB_ID(N'CloneEbayBuyer') IS NOT NULL
    THROW 51000, 'CloneEbayBuyer already exists. No changes were made. Use a different database name or explicitly remove it yourself.', 1;
GO
CREATE DATABASE CloneEbayBuyer;
GO
USE CloneEbayBuyer;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ===========================================================
-- A. ACCOUNT & AUTHENTICATION (4)
-- ===========================================================
CREATE TABLE dbo.[User] (
    id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_User PRIMARY KEY,
    username         NVARCHAR(100) NOT NULL,
    email            NVARCHAR(255) NOT NULL,
    passwordHash     NVARCHAR(500) NULL,  -- NULL permitted for social-only accounts
    role             NVARCHAR(20) NOT NULL CONSTRAINT DF_User_Role DEFAULT N'BUYER',
    avatarURL        NVARCHAR(1000) NULL,
    phoneNumber      NVARCHAR(30) NULL,
    emailVerified    BIT NOT NULL CONSTRAINT DF_User_EmailVerified DEFAULT (0),
    accountStatus    NVARCHAR(20) NOT NULL CONSTRAINT DF_User_AccountStatus DEFAULT N'ACTIVE',
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_User_CreatedAt DEFAULT SYSUTCDATETIME(),
    updatedAt        DATETIME2(3) NOT NULL CONSTRAINT DF_User_UpdatedAt DEFAULT SYSUTCDATETIME(),
    lastLoginAt      DATETIME2(3) NULL,
    authVersion      INT NOT NULL CONSTRAINT DF_User_AuthVersion DEFAULT (0),
    usernameChangedAt DATETIME2(3) NULL,
    CONSTRAINT UQ_User_Email UNIQUE (email),
    CONSTRAINT CK_User_Role CHECK (role IN (N'BUYER', N'SELLER', N'ADMIN')),
    CONSTRAINT CK_User_Status CHECK (accountStatus IN (N'ACTIVE', N'LOCKED', N'SUSPENDED', N'DELETED')),
    CONSTRAINT CK_User_Username CHECK (LEN(LTRIM(RTRIM(username))) > 0)
);
GO
CREATE INDEX IX_User_Username ON dbo.[User](username);
GO

CREATE TABLE dbo.ExternalLogin (
    id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExternalLogin PRIMARY KEY,
    userId           INT NOT NULL,
    provider         NVARCHAR(20) NOT NULL,
    providerUserId   NVARCHAR(255) NOT NULL,
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_ExternalLogin_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ExternalLogin_User FOREIGN KEY (userId) REFERENCES dbo.[User](id),
    CONSTRAINT UQ_ExternalLogin_ProviderIdentity UNIQUE (provider, providerUserId),
    CONSTRAINT UQ_ExternalLogin_UserProvider UNIQUE (userId, provider),
    CONSTRAINT CK_ExternalLogin_Provider CHECK (provider IN (N'GOOGLE', N'FACEBOOK'))
);
GO

CREATE TABLE dbo.VerificationToken (
    id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VerificationToken PRIMARY KEY,
    userId           INT NOT NULL,
    tokenType        NVARCHAR(30) NOT NULL,
    tokenHash        NVARCHAR(500) NOT NULL,
    expiresAt        DATETIME2(3) NOT NULL,
    usedAt           DATETIME2(3) NULL,
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_VerificationToken_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_VerificationToken_User FOREIGN KEY (userId) REFERENCES dbo.[User](id),
    CONSTRAINT UQ_VerificationToken_Hash UNIQUE (tokenHash),
    CONSTRAINT CK_VerificationToken_Type CHECK (tokenType IN (N'EMAIL_VERIFY', N'PASSWORD_RESET')),
    CONSTRAINT CK_VerificationToken_Expiry CHECK (expiresAt > createdAt)
);
GO
CREATE INDEX IX_VerificationToken_UserType ON dbo.VerificationToken(userId, tokenType, expiresAt);
GO

CREATE TABLE dbo.Address (
    id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Address PRIMARY KEY,
    userId           INT NOT NULL,
    fullName         NVARCHAR(150) NOT NULL,
    phone            NVARCHAR(30) NOT NULL,
    street           NVARCHAR(255) NOT NULL,
    addressLine2     NVARCHAR(255) NULL,
    city             NVARCHAR(100) NOT NULL,
    state            NVARCHAR(100) NULL,
    postalCode       NVARCHAR(30) NULL,
    country          NVARCHAR(100) NOT NULL,
    countryCode      NVARCHAR(10) NULL,
    isDefault        BIT NOT NULL CONSTRAINT DF_Address_IsDefault DEFAULT (0),
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_Address_CreatedAt DEFAULT SYSUTCDATETIME(),
    updatedAt        DATETIME2(3) NOT NULL CONSTRAINT DF_Address_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Address_User FOREIGN KEY (userId) REFERENCES dbo.[User](id),
    CONSTRAINT CK_Address_RequiredFields CHECK (
        LEN(LTRIM(RTRIM(fullName))) > 0 AND LEN(LTRIM(RTRIM(phone))) > 0
        AND LEN(LTRIM(RTRIM(street))) > 0 AND LEN(LTRIM(RTRIM(city))) > 0
        AND LEN(LTRIM(RTRIM(country))) > 0)
);
GO
CREATE INDEX IX_Address_User ON dbo.Address(userId);
CREATE UNIQUE INDEX UX_Address_OneDefaultPerUser ON dbo.Address(userId, isDefault) WHERE isDefault = 1;
GO

-- ===========================================================
-- B. CATALOG / LISTINGS / CART (8)
-- ===========================================================
CREATE TABLE dbo.Category (
    id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Category PRIMARY KEY,
    parentCategoryId INT NULL,
    name             NVARCHAR(150) NOT NULL,
    slug             NVARCHAR(180) NOT NULL,
    isActive         BIT NOT NULL CONSTRAINT DF_Category_IsActive DEFAULT (1),
    CONSTRAINT FK_Category_Parent FOREIGN KEY (parentCategoryId) REFERENCES dbo.Category(id),
    CONSTRAINT UQ_Category_Slug UNIQUE (slug),
    CONSTRAINT CK_Category_NoSelfParent CHECK (parentCategoryId IS NULL OR parentCategoryId <> id)
);
GO
CREATE INDEX IX_Category_Parent ON dbo.Category(parentCategoryId);
GO

CREATE TABLE dbo.Product (
    id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Product PRIMARY KEY,
    title            NVARCHAR(255) NOT NULL,
    description      NVARCHAR(MAX) NULL,
    categoryId       INT NOT NULL,
    brand            NVARCHAR(150) NULL,
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_Product_CreatedAt DEFAULT SYSUTCDATETIME(),
    updatedAt        DATETIME2(3) NOT NULL CONSTRAINT DF_Product_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Product_Category FOREIGN KEY (categoryId) REFERENCES dbo.Category(id),
    CONSTRAINT CK_Product_Title CHECK (LEN(LTRIM(RTRIM(title))) > 0)
);
GO
CREATE INDEX IX_Product_Category ON dbo.Product(categoryId);
CREATE INDEX IX_Product_Title ON dbo.Product(title);
GO

CREATE TABLE dbo.ProductImage (
    id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductImage PRIMARY KEY,
    productId        INT NOT NULL,
    imageUrl         NVARCHAR(1000) NOT NULL,
    sortOrder        INT NOT NULL CONSTRAINT DF_ProductImage_SortOrder DEFAULT (0),
    isPrimary        BIT NOT NULL CONSTRAINT DF_ProductImage_IsPrimary DEFAULT (0),
    CONSTRAINT FK_ProductImage_Product FOREIGN KEY (productId) REFERENCES dbo.Product(id),
    CONSTRAINT CK_ProductImage_SortOrder CHECK (sortOrder >= 0)
);
GO
CREATE INDEX IX_ProductImage_ProductOrder ON dbo.ProductImage(productId, sortOrder);
CREATE UNIQUE INDEX UX_ProductImage_OnePrimary ON dbo.ProductImage(productId, isPrimary) WHERE isPrimary = 1;
GO

-- Seller-specific shipping policy and options are defined here because Listing references them.
CREATE TABLE dbo.ShippingPolicy (
    id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ShippingPolicy PRIMARY KEY,
    sellerId         INT NOT NULL,
    name             NVARCHAR(150) NOT NULL,
    isActive         BIT NOT NULL CONSTRAINT DF_ShippingPolicy_IsActive DEFAULT (1),
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_ShippingPolicy_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ShippingPolicy_Seller FOREIGN KEY (sellerId) REFERENCES dbo.[User](id),
    CONSTRAINT UQ_ShippingPolicy_IdSeller UNIQUE (id, sellerId)
);
GO
CREATE INDEX IX_ShippingPolicy_Seller ON dbo.ShippingPolicy(sellerId);
GO

CREATE TABLE dbo.ShippingOption (
    id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ShippingOption PRIMARY KEY,
    shippingPolicyId INT NOT NULL,
    serviceName      NVARCHAR(150) NOT NULL,
    serviceType      NVARCHAR(20) NOT NULL,
    flatRate         DECIMAL(18,2) NOT NULL CONSTRAINT DF_ShippingOption_FlatRate DEFAULT (0),
    estimatedMinDays INT NULL,
    estimatedMaxDays INT NULL,
    isActive         BIT NOT NULL CONSTRAINT DF_ShippingOption_IsActive DEFAULT (1),
    CONSTRAINT FK_ShippingOption_Policy FOREIGN KEY (shippingPolicyId) REFERENCES dbo.ShippingPolicy(id),
    CONSTRAINT UQ_ShippingOption_IdPolicy UNIQUE (id, shippingPolicyId),
    CONSTRAINT CK_ShippingOption_Type CHECK (serviceType IN (N'STANDARD', N'EXPRESS', N'LOCAL_PICKUP')),
    CONSTRAINT CK_ShippingOption_Fee CHECK (flatRate >= 0),
    CONSTRAINT CK_ShippingOption_Days CHECK (
        (estimatedMinDays IS NULL OR estimatedMinDays >= 0) AND
        (estimatedMaxDays IS NULL OR estimatedMaxDays >= 0) AND
        (estimatedMinDays IS NULL OR estimatedMaxDays IS NULL OR estimatedMinDays <= estimatedMaxDays))
);
GO
CREATE INDEX IX_ShippingOption_Policy ON dbo.ShippingOption(shippingPolicyId);
GO

CREATE TABLE dbo.Listing (
    id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Listing PRIMARY KEY,
    productId        INT NULL,                  -- independent listings are allowed
    sellerId         INT NOT NULL,
    categoryId       INT NOT NULL,
    shippingPolicyId INT NULL,
    title            NVARCHAR(255) NOT NULL,
    description      NVARCHAR(MAX) NULL,
    itemCondition    NVARCHAR(50) NULL,
    listingType      NVARCHAR(20) NOT NULL,
    price            DECIMAL(18,2) NULL,       -- required for FIXED_PRICE; NULL for AUCTION
    quantity         INT NOT NULL CONSTRAINT DF_Listing_Quantity DEFAULT (1),
    status           NVARCHAR(20) NOT NULL CONSTRAINT DF_Listing_Status DEFAULT N'DRAFT',
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_Listing_CreatedAt DEFAULT SYSUTCDATETIME(),
    publishedAt      DATETIME2(3) NULL,
    endedAt          DATETIME2(3) NULL,
    rowVersion       ROWVERSION NOT NULL,       -- optimistic concurrency on stock/updates
    CONSTRAINT FK_Listing_Product FOREIGN KEY (productId) REFERENCES dbo.Product(id),
    CONSTRAINT FK_Listing_Seller FOREIGN KEY (sellerId) REFERENCES dbo.[User](id),
    CONSTRAINT FK_Listing_Category FOREIGN KEY (categoryId) REFERENCES dbo.Category(id),
    CONSTRAINT FK_Listing_ShippingPolicySeller FOREIGN KEY (shippingPolicyId, sellerId)
        REFERENCES dbo.ShippingPolicy(id, sellerId),
    CONSTRAINT CK_Listing_Type CHECK (listingType IN (N'FIXED_PRICE', N'AUCTION')),
    CONSTRAINT CK_Listing_Price CHECK (
        (listingType = N'FIXED_PRICE' AND price IS NOT NULL AND price >= 0)
        OR (listingType = N'AUCTION' AND price IS NULL)),
    CONSTRAINT CK_Listing_Quantity CHECK (quantity >= 0),
    CONSTRAINT CK_Listing_Status CHECK (status IN (N'DRAFT', N'ACTIVE', N'PAUSED', N'ENDED', N'SOLD', N'CANCELLED')),
    CONSTRAINT CK_Listing_Title CHECK (LEN(LTRIM(RTRIM(title))) > 0)
);
GO
CREATE INDEX IX_Listing_CategoryStatus ON dbo.Listing(categoryId, status, listingType);
CREATE INDEX IX_Listing_Product ON dbo.Listing(productId) WHERE productId IS NOT NULL;
CREATE INDEX IX_Listing_SellerStatus ON dbo.Listing(sellerId, status);
CREATE INDEX IX_Listing_StatusPrice ON dbo.Listing(status, listingType, price);
GO

CREATE TABLE dbo.ListingImage (
    id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ListingImage PRIMARY KEY,
    listingId        INT NOT NULL,
    imageUrl         NVARCHAR(1000) NOT NULL,
    sortOrder        INT NOT NULL CONSTRAINT DF_ListingImage_SortOrder DEFAULT (0),
    isPrimary        BIT NOT NULL CONSTRAINT DF_ListingImage_IsPrimary DEFAULT (0),
    CONSTRAINT FK_ListingImage_Listing FOREIGN KEY (listingId) REFERENCES dbo.Listing(id),
    CONSTRAINT CK_ListingImage_SortOrder CHECK (sortOrder >= 0)
);
GO
CREATE INDEX IX_ListingImage_ListingOrder ON dbo.ListingImage(listingId, sortOrder);
CREATE UNIQUE INDEX UX_ListingImage_OnePrimary ON dbo.ListingImage(listingId, isPrimary) WHERE isPrimary = 1;
GO

CREATE TABLE dbo.Cart (
    id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cart PRIMARY KEY,
    userId           INT NOT NULL,
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_Cart_CreatedAt DEFAULT SYSUTCDATETIME(),
    updatedAt        DATETIME2(3) NOT NULL CONSTRAINT DF_Cart_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Cart_User FOREIGN KEY (userId) REFERENCES dbo.[User](id),
    CONSTRAINT UQ_Cart_User UNIQUE (userId)
);
GO

CREATE TABLE dbo.CartItem (
    id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CartItem PRIMARY KEY,
    cartId           INT NOT NULL,
    listingId        INT NOT NULL,
    quantity         INT NOT NULL,
    status           NVARCHAR(20) NOT NULL CONSTRAINT DF_CartItem_Status DEFAULT N'ACTIVE',
    addedAt          DATETIME2(3) NOT NULL CONSTRAINT DF_CartItem_AddedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CartItem_Cart FOREIGN KEY (cartId) REFERENCES dbo.Cart(id) ON DELETE CASCADE,
    CONSTRAINT FK_CartItem_Listing FOREIGN KEY (listingId) REFERENCES dbo.Listing(id),
    CONSTRAINT UQ_CartItem_CartListing UNIQUE (cartId, listingId),
    CONSTRAINT CK_CartItem_Quantity CHECK (quantity > 0),
    CONSTRAINT CK_CartItem_Status CHECK (status IN (N'ACTIVE', N'SAVED_FOR_LATER'))
);
GO
CREATE INDEX IX_CartItem_CartStatus ON dbo.CartItem(cartId, status);
GO

CREATE TABLE dbo.Watchlist (
    id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Watchlist PRIMARY KEY,
    userId           INT NOT NULL,
    listingId        INT NOT NULL,
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_Watchlist_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Watchlist_User FOREIGN KEY (userId) REFERENCES dbo.[User](id),
    CONSTRAINT FK_Watchlist_Listing FOREIGN KEY (listingId) REFERENCES dbo.Listing(id),
    CONSTRAINT UQ_Watchlist_UserListing UNIQUE (userId, listingId)
);
GO
CREATE INDEX IX_Watchlist_Listing ON dbo.Watchlist(listingId);
GO

-- ===========================================================
-- C. AUCTION & BIDDING (2)
-- ===========================================================
CREATE TABLE dbo.Auction (
    id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Auction PRIMARY KEY,
    listingId        INT NOT NULL,
    startingPrice    DECIMAL(18,2) NOT NULL,
    currentPrice     DECIMAL(18,2) NOT NULL,
    bidIncrement     DECIMAL(18,2) NOT NULL,
    startTime        DATETIME2(3) NOT NULL,
    endTime          DATETIME2(3) NOT NULL,
    highestBidderId  INT NULL,
    winnerId         INT NULL,
    winningPrice     DECIMAL(18,2) NULL,
    bidCount         INT NOT NULL CONSTRAINT DF_Auction_BidCount DEFAULT (0),
    status           NVARCHAR(20) NOT NULL CONSTRAINT DF_Auction_Status DEFAULT N'SCHEDULED',
    resolvedAt       DATETIME2(3) NULL,
    rowVersion       ROWVERSION NOT NULL,
    CONSTRAINT FK_Auction_Listing FOREIGN KEY (listingId) REFERENCES dbo.Listing(id),
    CONSTRAINT FK_Auction_HighestBidder FOREIGN KEY (highestBidderId) REFERENCES dbo.[User](id),
    CONSTRAINT FK_Auction_Winner FOREIGN KEY (winnerId) REFERENCES dbo.[User](id),
    CONSTRAINT UQ_Auction_Listing UNIQUE (listingId),
    CONSTRAINT CK_Auction_Prices CHECK (startingPrice >= 0 AND currentPrice >= startingPrice AND bidIncrement > 0 AND (winningPrice IS NULL OR winningPrice >= startingPrice)),
    CONSTRAINT CK_Auction_Time CHECK (startTime < endTime),
    CONSTRAINT CK_Auction_BidCount CHECK (bidCount >= 0),
    CONSTRAINT CK_Auction_Status CHECK (status IN (N'SCHEDULED', N'ACTIVE', N'ENDED', N'CANCELLED')),
    CONSTRAINT CK_Auction_Winner CHECK (winnerId IS NULL OR winningPrice IS NOT NULL)
);
GO
CREATE INDEX IX_Auction_StatusEndTime ON dbo.Auction(status, endTime);
GO

CREATE TABLE dbo.Bid (
    id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bid PRIMARY KEY,
    auctionId        INT NOT NULL,
    bidderId         INT NOT NULL,
    maximumBid       DECIMAL(18,2) NOT NULL,
    effectiveBid     DECIMAL(18,2) NOT NULL,
    status           NVARCHAR(20) NOT NULL CONSTRAINT DF_Bid_Status DEFAULT N'ACTIVE',
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_Bid_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Bid_Auction FOREIGN KEY (auctionId) REFERENCES dbo.Auction(id),
    CONSTRAINT FK_Bid_Bidder FOREIGN KEY (bidderId) REFERENCES dbo.[User](id),
    CONSTRAINT CK_Bid_Amounts CHECK (maximumBid > 0 AND effectiveBid >= 0 AND effectiveBid <= maximumBid),
    CONSTRAINT CK_Bid_Status CHECK (status IN (N'ACTIVE', N'OUTBID', N'WINNING', N'WON', N'RETRACTED', N'CANCELLED'))
);
GO
CREATE INDEX IX_Bid_AuctionMaximum ON dbo.Bid(auctionId, maximumBid DESC, createdAt ASC, id ASC);
CREATE INDEX IX_Bid_BidderCreated ON dbo.Bid(bidderId, createdAt DESC);
GO

-- ===========================================================
-- D. COUPON / MULTI-SELLER CHECKOUT / PAYMENTS (7)
-- ===========================================================
CREATE TABLE dbo.Coupon (
    id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Coupon PRIMARY KEY,
    code              NVARCHAR(50) NOT NULL,
    discountType      NVARCHAR(20) NOT NULL,
    discountValue     DECIMAL(18,2) NOT NULL,
    minOrderValue     DECIMAL(18,2) NULL,
    maxDiscountAmount DECIMAL(18,2) NULL,
    startDate         DATETIME2(3) NOT NULL,
    endDate           DATETIME2(3) NOT NULL,
    maxUsage          INT NULL,
    usageCount        INT NOT NULL CONSTRAINT DF_Coupon_UsageCount DEFAULT (0),
    isActive          BIT NOT NULL CONSTRAINT DF_Coupon_IsActive DEFAULT (1),
    CONSTRAINT UQ_Coupon_Code UNIQUE (code),
    CONSTRAINT CK_Coupon_Type CHECK (discountType IN (N'PERCENT', N'FIXED_AMOUNT')),
    CONSTRAINT CK_Coupon_Discount CHECK (
        discountValue > 0 AND
        (discountType = N'FIXED_AMOUNT' OR (discountType = N'PERCENT' AND discountValue <= 100))),
    CONSTRAINT CK_Coupon_Minimum CHECK (minOrderValue IS NULL OR minOrderValue >= 0),
    CONSTRAINT CK_Coupon_Cap CHECK (maxDiscountAmount IS NULL OR maxDiscountAmount > 0),
    CONSTRAINT CK_Coupon_Dates CHECK (startDate < endDate),
    CONSTRAINT CK_Coupon_Usage CHECK ((maxUsage IS NULL OR maxUsage > 0) AND usageCount >= 0 AND (maxUsage IS NULL OR usageCount <= maxUsage))
);
GO

CREATE TABLE dbo.CheckoutGroup (
    id                BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CheckoutGroup PRIMARY KEY,
    buyerId           INT NULL,
    guestEmail        NVARCHAR(255) NULL,
    couponId          INT NULL,
    subtotal          DECIMAL(18,2) NOT NULL,
    discountAmount    DECIMAL(18,2) NOT NULL CONSTRAINT DF_CheckoutGroup_Discount DEFAULT (0),
    shippingFee       DECIMAL(18,2) NOT NULL CONSTRAINT DF_CheckoutGroup_Shipping DEFAULT (0),
    taxAmount          DECIMAL(18,2) NOT NULL CONSTRAINT DF_Group_Tax DEFAULT (0),
    totalAmount       DECIMAL(18,2) NOT NULL,
    currency          NVARCHAR(10) NOT NULL CONSTRAINT DF_CheckoutGroup_Currency DEFAULT N'VND',
    status            NVARCHAR(20) NOT NULL CONSTRAINT DF_CheckoutGroup_Status DEFAULT N'PENDING',
    paymentDeadline   DATETIME2(3) NULL,  -- PayPal mock only; COD = NULL
    inventoryReleasedAt DATETIME2(3) NULL, -- aggregate marker; per-order marker is authoritative
    createdAt         DATETIME2(3) NOT NULL CONSTRAINT DF_CheckoutGroup_CreatedAt DEFAULT SYSUTCDATETIME(),
    confirmedAt       DATETIME2(3) NULL,
    CONSTRAINT FK_CheckoutGroup_Buyer FOREIGN KEY (buyerId) REFERENCES dbo.[User](id),
    CONSTRAINT FK_CheckoutGroup_Coupon FOREIGN KEY (couponId) REFERENCES dbo.Coupon(id),
    CONSTRAINT CK_CheckoutGroup_Identity CHECK (
        (buyerId IS NOT NULL AND guestEmail IS NULL) OR
        (buyerId IS NULL AND guestEmail IS NOT NULL AND LEN(LTRIM(RTRIM(guestEmail))) > 0)),
    CONSTRAINT CK_CheckoutGroup_Money CHECK (
        subtotal >= 0 AND discountAmount >= 0 AND discountAmount <= subtotal
        AND shippingFee >= 0 AND taxAmount >= 0 AND totalAmount >= 0
        AND totalAmount = subtotal - discountAmount + shippingFee + taxAmount),
    CONSTRAINT CK_CheckoutGroup_Status CHECK (status IN (N'PENDING', N'CONFIRMED', N'CANCELLED', N'EXPIRED')),
    CONSTRAINT CK_CheckoutGroup_Deadline CHECK (paymentDeadline IS NULL OR paymentDeadline > createdAt)
);
GO
CREATE INDEX IX_CheckoutGroup_BuyerCreated ON dbo.CheckoutGroup(buyerId, createdAt DESC) WHERE buyerId IS NOT NULL;
CREATE INDEX IX_CheckoutGroup_GuestEmail ON dbo.CheckoutGroup(guestEmail) WHERE guestEmail IS NOT NULL;
CREATE INDEX IX_CheckoutGroup_Expiry ON dbo.CheckoutGroup(status, paymentDeadline) WHERE paymentDeadline IS NOT NULL;
GO

CREATE TABLE dbo.OrderTable (
    id                   BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderTable PRIMARY KEY,
    checkoutGroupId      BIGINT NOT NULL,
    buyerId              INT NULL, -- denormalized for account-order lookup; guest remains NULL
    sellerId             INT NOT NULL,
    addressId            INT NULL, -- nullable: guest/manual shipping snapshot; not required after address deletion
    shippingOptionId     INT NULL,
    shippingFullName     NVARCHAR(150) NOT NULL,
    shippingEmail        NVARCHAR(255) NOT NULL,
    shippingPhone        NVARCHAR(30) NOT NULL,
    shippingStreet       NVARCHAR(255) NOT NULL,
    shippingAddressLine2 NVARCHAR(255) NULL,
    shippingCity         NVARCHAR(100) NOT NULL,
    shippingState        NVARCHAR(100) NULL,
    shippingPostalCode   NVARCHAR(30) NULL,
    shippingCountry      NVARCHAR(100) NOT NULL,
    shippingCountryCode  NVARCHAR(10) NULL,
    shippingServiceName  NVARCHAR(150) NULL,
    subtotal             DECIMAL(18,2) NOT NULL,
    discountAmount       DECIMAL(18,2) NOT NULL CONSTRAINT DF_Order_Discount DEFAULT (0),
    shippingFee          DECIMAL(18,2) NOT NULL CONSTRAINT DF_Order_Shipping DEFAULT (0),
    taxAmount             DECIMAL(18,2) NOT NULL CONSTRAINT DF_Order_Tax DEFAULT (0),
    totalAmount          DECIMAL(18,2) NOT NULL,
    currency             NVARCHAR(10) NOT NULL CONSTRAINT DF_Order_Currency DEFAULT N'VND',
    status               NVARCHAR(20) NOT NULL CONSTRAINT DF_Order_Status DEFAULT N'PLACED',
    stockRestoredAt      DATETIME2(3) NULL, -- exactly-once stock return, set in same transaction as increment
    orderDate            DATETIME2(3) NOT NULL CONSTRAINT DF_Order_Date DEFAULT SYSUTCDATETIME(),
    updatedAt            DATETIME2(3) NOT NULL CONSTRAINT DF_Order_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Order_CheckoutGroup FOREIGN KEY (checkoutGroupId) REFERENCES dbo.CheckoutGroup(id),
    CONSTRAINT FK_Order_Buyer FOREIGN KEY (buyerId) REFERENCES dbo.[User](id),
    CONSTRAINT FK_Order_Seller FOREIGN KEY (sellerId) REFERENCES dbo.[User](id),
    CONSTRAINT FK_Order_Address FOREIGN KEY (addressId) REFERENCES dbo.Address(id),
    CONSTRAINT FK_Order_ShippingOption FOREIGN KEY (shippingOptionId) REFERENCES dbo.ShippingOption(id),
    CONSTRAINT CK_Order_Money CHECK (
        subtotal >= 0 AND discountAmount >= 0 AND discountAmount <= subtotal
        AND shippingFee >= 0 AND taxAmount >= 0 AND totalAmount >= 0
        AND totalAmount = subtotal - discountAmount + shippingFee + taxAmount),
    CONSTRAINT CK_Order_Status CHECK (status IN (N'PLACED', N'CONFIRMED', N'COMPLETED', N'CANCELLED')),
    CONSTRAINT CK_Order_Contact CHECK (
        LEN(LTRIM(RTRIM(shippingFullName))) > 0 AND LEN(LTRIM(RTRIM(shippingEmail))) > 0
        AND LEN(LTRIM(RTRIM(shippingPhone))) > 0 AND LEN(LTRIM(RTRIM(shippingStreet))) > 0
        AND LEN(LTRIM(RTRIM(shippingCity))) > 0 AND LEN(LTRIM(RTRIM(shippingCountry))) > 0)
);
GO
CREATE INDEX IX_Order_CheckoutGroup ON dbo.OrderTable(checkoutGroupId);
CREATE INDEX IX_Order_BuyerCreated ON dbo.OrderTable(buyerId, orderDate DESC) WHERE buyerId IS NOT NULL;
CREATE INDEX IX_Order_SellerStatus ON dbo.OrderTable(sellerId, status, orderDate DESC);
GO

CREATE TABLE dbo.OrderItem (
    id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderItem PRIMARY KEY,
    orderId          BIGINT NOT NULL,
    listingId        INT NOT NULL,
    productId        INT NULL, -- matches Listing.productId AT PURCHASE TIME
    quantity         INT NOT NULL,
    unitPrice        DECIMAL(18,2) NOT NULL,
    productTitle     NVARCHAR(255) NOT NULL, -- snapshot of Listing.title
    imageUrl         NVARCHAR(1000) NULL, -- snapshot of primary listing image
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_OrderItem_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_OrderItem_Order FOREIGN KEY (orderId) REFERENCES dbo.OrderTable(id),
    CONSTRAINT FK_OrderItem_Listing FOREIGN KEY (listingId) REFERENCES dbo.Listing(id),
    CONSTRAINT FK_OrderItem_Product FOREIGN KEY (productId) REFERENCES dbo.Product(id),
    CONSTRAINT CK_OrderItem_Quantity CHECK (quantity > 0),
    CONSTRAINT CK_OrderItem_Price CHECK (unitPrice >= 0)
);
GO
CREATE INDEX IX_OrderItem_Order ON dbo.OrderItem(orderId);
CREATE INDEX IX_OrderItem_Listing ON dbo.OrderItem(listingId);
GO

CREATE TABLE dbo.Payment (
    id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payment PRIMARY KEY,
    checkoutGroupId  BIGINT NOT NULL,
    amount           DECIMAL(18,2) NOT NULL,
    method           NVARCHAR(20) NOT NULL,
    provider         NVARCHAR(50) NULL,
    transactionId    NVARCHAR(150) NULL,
    status           NVARCHAR(20) NOT NULL CONSTRAINT DF_Payment_Status DEFAULT N'PENDING',
    failureReason    NVARCHAR(500) NULL,
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_Payment_CreatedAt DEFAULT SYSUTCDATETIME(),
    paidAt           DATETIME2(3) NULL,
    CONSTRAINT FK_Payment_CheckoutGroup FOREIGN KEY (checkoutGroupId) REFERENCES dbo.CheckoutGroup(id),
    CONSTRAINT CK_Payment_Amount CHECK (amount >= 0),
    CONSTRAINT CK_Payment_Method CHECK (method IN (N'PAYPAL', N'COD')),
    CONSTRAINT CK_Payment_Status CHECK (status IN (N'PENDING', N'PROCESSING', N'PAID', N'FAILED')),
    CONSTRAINT CK_Payment_COD CHECK (method <> N'COD' OR status = N'PENDING'),
    CONSTRAINT CK_Payment_PaidAt CHECK ((status = N'PAID' AND paidAt IS NOT NULL) OR (status <> N'PAID' AND paidAt IS NULL))
);
GO
CREATE INDEX IX_Payment_GroupCreated ON dbo.Payment(checkoutGroupId, createdAt DESC);
CREATE UNIQUE INDEX UX_Payment_TransactionId ON dbo.Payment(transactionId) WHERE transactionId IS NOT NULL;
CREATE UNIQUE INDEX UX_Payment_OnePaidPerCheckout ON dbo.Payment(checkoutGroupId, status) WHERE status = N'PAID';
CREATE UNIQUE INDEX UX_Payment_OneCODPerCheckout ON dbo.Payment(checkoutGroupId, method) WHERE method = N'COD';
GO

CREATE TABLE dbo.GuestCheckoutAccess (
    id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GuestCheckoutAccess PRIMARY KEY,
    checkoutGroupId  BIGINT NOT NULL,
    tokenHash        NVARCHAR(500) NOT NULL,
    expiresAt        DATETIME2(3) NOT NULL,
    revokedAt        DATETIME2(3) NULL,
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_GuestCheckoutAccess_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_GuestCheckoutAccess_Group FOREIGN KEY (checkoutGroupId) REFERENCES dbo.CheckoutGroup(id),
    CONSTRAINT UQ_GuestCheckoutAccess_TokenHash UNIQUE (tokenHash),
    CONSTRAINT CK_GuestCheckoutAccess_Expiry CHECK (expiresAt > createdAt)
);
GO
CREATE INDEX IX_GuestCheckoutAccess_Group ON dbo.GuestCheckoutAccess(checkoutGroupId, expiresAt);
GO

CREATE TABLE dbo.CouponUsage (
    id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CouponUsage PRIMARY KEY,
    couponId         INT NOT NULL,
    checkoutGroupId  BIGINT NOT NULL,
    userId           INT NULL,
    discountAmount   DECIMAL(18,2) NOT NULL,
    usedAt           DATETIME2(3) NOT NULL CONSTRAINT DF_CouponUsage_UsedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CouponUsage_Coupon FOREIGN KEY (couponId) REFERENCES dbo.Coupon(id),
    CONSTRAINT FK_CouponUsage_Group FOREIGN KEY (checkoutGroupId) REFERENCES dbo.CheckoutGroup(id),
    CONSTRAINT FK_CouponUsage_User FOREIGN KEY (userId) REFERENCES dbo.[User](id),
    CONSTRAINT UQ_CouponUsage_Group UNIQUE (checkoutGroupId),
    CONSTRAINT CK_CouponUsage_Discount CHECK (discountAmount >= 0)
);
GO
CREATE INDEX IX_CouponUsage_CouponUser ON dbo.CouponUsage(couponId, userId);
GO

-- ===========================================================
-- E. SHIPPING / AFTER-SALES (4)
-- ===========================================================
CREATE TABLE dbo.ShippingInfo (
    id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ShippingInfo PRIMARY KEY,
    orderId          BIGINT NOT NULL,
    carrier          NVARCHAR(100) NULL,
    trackingNumber   NVARCHAR(150) NULL,
    status           NVARCHAR(20) NOT NULL CONSTRAINT DF_ShippingInfo_Status DEFAULT N'PENDING',
    shippedAt        DATETIME2(3) NULL,
    estimatedArrival DATETIME2(3) NULL,
    deliveredAt      DATETIME2(3) NULL,
    updatedAt        DATETIME2(3) NOT NULL CONSTRAINT DF_ShippingInfo_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ShippingInfo_Order FOREIGN KEY (orderId) REFERENCES dbo.OrderTable(id),
    CONSTRAINT UQ_ShippingInfo_Order UNIQUE (orderId),
    CONSTRAINT CK_ShippingInfo_Status CHECK (status IN (N'PENDING', N'LABEL_CREATED', N'IN_TRANSIT', N'DELIVERED', N'FAILED', N'RETURNED')),
    CONSTRAINT CK_ShippingInfo_Delivery CHECK (deliveredAt IS NULL OR shippedAt IS NULL OR deliveredAt >= shippedAt)
);
GO
CREATE INDEX IX_ShippingInfo_Tracking ON dbo.ShippingInfo(trackingNumber) WHERE trackingNumber IS NOT NULL;
GO

CREATE TABLE dbo.CancellationRequest (
    id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CancellationRequest PRIMARY KEY,
    orderId          BIGINT NOT NULL,
    requestedBy      INT NOT NULL,
    reason           NVARCHAR(1000) NOT NULL,
    status           NVARCHAR(20) NOT NULL CONSTRAINT DF_CancellationRequest_Status DEFAULT N'PENDING',
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_CancellationRequest_CreatedAt DEFAULT SYSUTCDATETIME(),
    resolvedAt       DATETIME2(3) NULL,
    CONSTRAINT FK_CancellationRequest_Order FOREIGN KEY (orderId) REFERENCES dbo.OrderTable(id),
    CONSTRAINT FK_CancellationRequest_User FOREIGN KEY (requestedBy) REFERENCES dbo.[User](id),
    CONSTRAINT CK_CancellationRequest_Status CHECK (status IN (N'PENDING', N'APPROVED', N'REJECTED', N'CANCELLED')),
    CONSTRAINT CK_CancellationRequest_Reason CHECK (LEN(LTRIM(RTRIM(reason))) > 0)
);
GO
CREATE UNIQUE INDEX UX_CancellationRequest_OnePendingPerOrder ON dbo.CancellationRequest(orderId, status) WHERE status = N'PENDING';
CREATE INDEX IX_CancellationRequest_RequestedBy ON dbo.CancellationRequest(requestedBy, createdAt DESC);
GO

CREATE TABLE dbo.ReturnRequest (
    id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReturnRequest PRIMARY KEY,
    orderId          BIGINT NOT NULL,
    requestedBy      INT NOT NULL,
    reasonCode       NVARCHAR(50) NOT NULL,
    reasonDetail     NVARCHAR(MAX) NULL,
    status           NVARCHAR(20) NOT NULL CONSTRAINT DF_ReturnRequest_Status DEFAULT N'REQUESTED',
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_ReturnRequest_CreatedAt DEFAULT SYSUTCDATETIME(),
    approvedAt       DATETIME2(3) NULL,
    completedAt      DATETIME2(3) NULL,
    CONSTRAINT FK_ReturnRequest_Order FOREIGN KEY (orderId) REFERENCES dbo.OrderTable(id),
    CONSTRAINT FK_ReturnRequest_User FOREIGN KEY (requestedBy) REFERENCES dbo.[User](id),
    CONSTRAINT CK_ReturnRequest_Status CHECK (status IN (N'REQUESTED', N'APPROVED', N'REJECTED', N'RETURNING', N'COMPLETED', N'CANCELLED'))
);
GO
CREATE INDEX IX_ReturnRequest_OrderCreated ON dbo.ReturnRequest(orderId, createdAt DESC);
CREATE INDEX IX_ReturnRequest_RequestedBy ON dbo.ReturnRequest(requestedBy, createdAt DESC);
GO

CREATE TABLE dbo.ReturnRequestItem (
    id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReturnRequestItem PRIMARY KEY,
    returnRequestId  BIGINT NOT NULL,
    orderItemId      BIGINT NOT NULL,
    quantity         INT NOT NULL,
    CONSTRAINT FK_ReturnRequestItem_Request FOREIGN KEY (returnRequestId) REFERENCES dbo.ReturnRequest(id),
    CONSTRAINT FK_ReturnRequestItem_OrderItem FOREIGN KEY (orderItemId) REFERENCES dbo.OrderItem(id),
    CONSTRAINT UQ_ReturnRequestItem_RequestItem UNIQUE (returnRequestId, orderItemId),
    CONSTRAINT CK_ReturnRequestItem_Quantity CHECK (quantity > 0)
);
GO
CREATE INDEX IX_ReturnRequestItem_OrderItem ON dbo.ReturnRequestItem(orderItemId);
GO

-- ===========================================================
-- F. REVIEWS / FEEDBACK / NOTIFICATIONS (4)
-- ===========================================================
CREATE TABLE dbo.Review (
    id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Review PRIMARY KEY,
    orderItemId      BIGINT NOT NULL,
    productId        INT NOT NULL,       -- independent listings are not product-reviewable
    reviewerId       INT NOT NULL,
    rating           INT NOT NULL,
    comment          NVARCHAR(MAX) NULL,
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_Review_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Review_OrderItem FOREIGN KEY (orderItemId) REFERENCES dbo.OrderItem(id),
    CONSTRAINT FK_Review_Product FOREIGN KEY (productId) REFERENCES dbo.Product(id),
    CONSTRAINT FK_Review_User FOREIGN KEY (reviewerId) REFERENCES dbo.[User](id),
    CONSTRAINT UQ_Review_OrderItem UNIQUE (orderItemId),
    CONSTRAINT CK_Review_Rating CHECK (rating BETWEEN 1 AND 5)
);
GO
CREATE INDEX IX_Review_ProductCreated ON dbo.Review(productId, createdAt DESC);
GO

CREATE TABLE dbo.SellerFeedback (
    id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SellerFeedback PRIMARY KEY,
    orderId          BIGINT NOT NULL,
    sellerId         INT NOT NULL,
    buyerId          INT NOT NULL,
    feedbackType     NVARCHAR(20) NOT NULL,
    comment          NVARCHAR(MAX) NULL,
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_SellerFeedback_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_SellerFeedback_Order FOREIGN KEY (orderId) REFERENCES dbo.OrderTable(id),
    CONSTRAINT FK_SellerFeedback_Seller FOREIGN KEY (sellerId) REFERENCES dbo.[User](id),
    CONSTRAINT FK_SellerFeedback_Buyer FOREIGN KEY (buyerId) REFERENCES dbo.[User](id),
    CONSTRAINT UQ_SellerFeedback_Order UNIQUE (orderId),
    CONSTRAINT CK_SellerFeedback_Type CHECK (feedbackType IN (N'POSITIVE', N'NEUTRAL', N'NEGATIVE')),
    CONSTRAINT CK_SellerFeedback_DifferentUsers CHECK (buyerId <> sellerId)
);
GO
CREATE INDEX IX_SellerFeedback_SellerCreated ON dbo.SellerFeedback(sellerId, createdAt DESC);
GO

CREATE TABLE dbo.BuyerFeedback (
    id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BuyerFeedback PRIMARY KEY,
    orderId          BIGINT NOT NULL,
    buyerId          INT NOT NULL,
    sellerId         INT NOT NULL,
    comment          NVARCHAR(MAX) NOT NULL,
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_BuyerFeedback_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_BuyerFeedback_Order FOREIGN KEY (orderId) REFERENCES dbo.OrderTable(id),
    CONSTRAINT FK_BuyerFeedback_Buyer FOREIGN KEY (buyerId) REFERENCES dbo.[User](id),
    CONSTRAINT FK_BuyerFeedback_Seller FOREIGN KEY (sellerId) REFERENCES dbo.[User](id),
    CONSTRAINT UQ_BuyerFeedback_Order UNIQUE (orderId),
    CONSTRAINT CK_BuyerFeedback_Comment CHECK (LEN(LTRIM(RTRIM(comment))) > 0),
    CONSTRAINT CK_BuyerFeedback_DifferentUsers CHECK (buyerId <> sellerId)
);
GO
CREATE INDEX IX_BuyerFeedback_BuyerCreated ON dbo.BuyerFeedback(buyerId, createdAt DESC, id DESC);
GO

CREATE TABLE dbo.Notification (
    id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notification PRIMARY KEY,
    userId           INT NOT NULL,
    type             NVARCHAR(40) NOT NULL,
    title            NVARCHAR(255) NOT NULL,
    content          NVARCHAR(MAX) NULL,
    referenceType    NVARCHAR(50) NULL,
    referenceId      NVARCHAR(100) NULL,
    isRead           BIT NOT NULL CONSTRAINT DF_Notification_IsRead DEFAULT (0),
    createdAt        DATETIME2(3) NOT NULL CONSTRAINT DF_Notification_CreatedAt DEFAULT SYSUTCDATETIME(),
    readAt           DATETIME2(3) NULL,
    CONSTRAINT FK_Notification_User FOREIGN KEY (userId) REFERENCES dbo.[User](id),
    CONSTRAINT CK_Notification_ReadAt CHECK ((isRead = 0 AND readAt IS NULL) OR (isRead = 1 AND readAt IS NOT NULL))
);
GO
CREATE INDEX IX_Notification_UserUnread ON dbo.Notification(userId, isRead, createdAt DESC);
GO

CREATE TABLE dbo.OperationReceipt (
    scope         NVARCHAR(256) NOT NULL,
    operationKey  NVARCHAR(100) NOT NULL,
    requestHash   NVARCHAR(64) NOT NULL,
    responseJson  NVARCHAR(MAX) NOT NULL,
    createdAt     DATETIME2 NOT NULL CONSTRAINT DF_OperationReceipt_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_OperationReceipt PRIMARY KEY (scope, operationKey)
);
GO

CREATE TABLE dbo.EmailOutbox (
    id         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmailOutbox PRIMARY KEY,
    recipient  NVARCHAR(255) NOT NULL,
    subject    NVARCHAR(200) NOT NULL,
    body       NVARCHAR(MAX) NOT NULL,
    createdAt  DATETIME2 NOT NULL CONSTRAINT DF_EmailOutbox_CreatedAt DEFAULT SYSUTCDATETIME(),
    sentAt     DATETIME2 NULL,
    attempts   INT NOT NULL CONSTRAINT DF_EmailOutbox_Attempts DEFAULT (0)
);
GO

-- ===========================================================
-- VIEW - includes stand-alone listings without Product
-- ===========================================================
CREATE VIEW dbo.vw_ActiveListings
AS
SELECT
    l.id AS listingId,
    l.productId,
    l.sellerId,
    l.categoryId,
    l.title,
    l.description,
    l.itemCondition,
    l.listingType,
    l.price,
    l.quantity,
    l.status,
    l.publishedAt,
    p.title AS catalogProductTitle,
    p.brand AS catalogBrand,
    a.id AS auctionId,
    a.currentPrice,
    a.startTime AS auctionStartTime,
    a.endTime AS auctionEndTime,
    a.bidCount,
    a.highestBidderId
FROM dbo.Listing AS l
LEFT JOIN dbo.Product AS p ON p.id = l.productId
LEFT JOIN dbo.Auction AS a ON a.listingId = l.id
WHERE l.status = N'ACTIVE';
GO

-- ===========================================================
-- VERIFICATION
-- ===========================================================
SELECT COUNT(*) AS tableCount
FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo'); -- expected 33
SELECT COUNT(*) AS foreignKeyCount
FROM sys.foreign_keys WHERE schema_id = SCHEMA_ID('dbo');
SELECT name AS viewName FROM sys.views WHERE schema_id = SCHEMA_ID('dbo');
GO

/*
APPLICATION SERVICE / TRANSACTION RULES (NOT AUTOMATICALLY ENFORCED BY THIS DDL)

1. LOGIN & USER OWNERSHIP:
   - Only eligible registered buyers bid, review and access account-only resources.
   - Google/Facebook first login may provision a Buyer account; email verification
     semantics for provider-verified addresses must be decided in Auth service.
   - Store only password hashes and cryptographic hashes of random access tokens.
   - UpdatedAt columns need explicit updates in application code (no SQL trigger).

2. LISTING & PRODUCT:
   - Listing.productId may be NULL. Listing.categoryId is canonical for filtering;
     verify compatible Category when Product is linked.
   - ACTIVE listings must have at least one ListingImage (application check).
   - FIXED_PRICE listings participate in Cart/Checkout. Auction listings do not.
   - AUCTION listing must have one Auction row; don't display an orphan auction listing.
   - This schema does not implement Best Offer, seller shipping-region restrictions,
     shipping-by-weight, region taxes, or live payment gateways.

3. STOCK:
   - Listing.quantity is available, sellable quantity: never let it fall below 0.
   - On Order creation, in ONE DB TRANSACTION: validate each ACTIVE fixed-price
     Listing and seller, recalculate totals, UPDATE Listing SET quantity=quantity-@n
     WHERE id=@listingId AND quantity>=@n AND status='ACTIVE', check rows affected,
     insert CheckoutGroup/OrderTable/OrderItem, then commit or rollback ALL.
   - On cancellation/expiry, restore stock ONCE per applicable Order, in a transaction
     and set OrderTable.stockRestoredAt. Do not restore again on repeated callbacks.
   - Order creation is not itself a payment attempt; create Payment records separately.
   - PayPal mock failed attempt does not automatically cancel the checkout;
     expiry job cancels checkout when paymentDeadline elapses and restores stock.
   - COD: paymentDeadline NULL and Payment=PENDING; no automatic PayPal expiry.

4. CHECKOUT / PAYMENT:
   - Exactly ONE checkout action may create multiple seller-specific OrderTable rows.
   - Sum(OrderTable totals) = CheckoutGroup totals (subtotal, discount, shipping,
     total); allocate discount with consistent rounding (remainder to last order).
   - One payment attempt is for the entire CheckoutGroup. PayPal simulation success
     marks ONE Payment PAID and the group CONFIRMED; failure writes FAILED and group
     stays PENDING until retry/deadline. COD writes PENDING, group CONFIRMED.
   - Prevent duplicate payment attempts from succeeding with idempotent transaction,
     row locking, UX_Payment_OnePaidPerCheckout, and a check of group state/deadline.
   - Payment.amount equals CheckoutGroup.totalAmount at time of each attempt.
   - Keep CheckoutGroup.status separate from Payment.status; CONFIRMED != PAID.
   - After GROUP confirmation individual orders can be CONFIRMED; cancellation and
     shipping remain order-level concerns, including partial order cancellation.
   - CouponUsage is one per CheckoutGroup. For a failed PayPal checkout, commit
     usage when group first confirms (NOT at order creation); enforce maxUsage in
     serializable transaction; COD counts when confirmed. No seller coupons.
   - Guest: validate access token by hash + expiry + revokedAt, check group is guest
     and is eligible to retry. Do not expose tokenHash or send raw token in logs.

5. AUCTION:
   - One Auction per AUCTION Listing; seller cannot bid on their own listing.
   - Bid maximum stays private. Proxy bidding, stable tie break (earliest bid),
     price increments, highestBidder and bidCount are computed in a transaction
     using Auction.rowVersion and then signaled AFTER commit.
   - Auction close worker sets winner, winningPrice, status and resolvedAt ONCE.
   - Winning an auction does not auto-create an Order in this Buyer scope.

6. ADDRESS / SHIPPING:
   - Guest address/contact is stored as Order snapshot; Registered Buyer may choose
     a saved Address or enter a new address at checkout.
   - Verify OrderTable.addressId belongs to OrderTable.buyerId (if provided).
   - Verify selected ShippingOption belongs to the associated Listing's Seller
     policy, is active, and is valid for all items in seller order.
   - ShippingInfo is one per Order (optional until created); only DELIVERED orders
     are eligible for Product Review / Seller Feedback.

7. REVIEWS / RETURNS / REQUESTS:
   - Review requires an authenticated Buyer matching OrderTable.buyerId, matching
     ProductId in OrderItem (non-null), and ShippingInfo.status='DELIVERED'.
   - SellerFeedback is Buyer-to-Seller and must match Order.sellerId and
     buyerId=Order.buyerId with delivered shipping.
   - BuyerFeedback is read-only in the Buyer module. It represents positive
     Seller-to-Buyer feedback and must match Order.sellerId and Order.buyerId
     with delivered shipping. Creation belongs to the Seller module.
   - Independent Listing still supports SellerFeedback and BuyerFeedback.
   - CancellationRequest/ReturnRequest requestedBy must own the Order; returns
     may reference only items of THAT order; return qty <= purchased qty and
     cumulative active return quantity must not exceed purchased quantity.
   - Pending requests are saved without automatic seller/admin approval workflows.
   - Guest checkout doesn't have cancellation/return/review in this scope.

8. CART / NOTIFICATIONS:
   - Guest cart and Saved for Later live in browser LocalStorage. Registered Buyer
     Cart/CartItem live in database; only ACTIVE items are eligible for checkout.
   - Cart merge on login remains an application design decision; no new DB table.
   - Notification referenceType/referenceId is a soft polymorphic reference, not FK.

9. SEEDS AND DEPLOYMENT:
   - No real users, products, coupons or demo data inserted by this script.
   - A Seller is represented by User.role='SELLER' with seed data maintained
     separately by the team; Seller/Admin modules need not exist.
   - SQL Server supports IDENTITY, filtered indexes, ROWVERSION and GO.
   - NO DROP statements. Execute ONLY on a dedicated fresh database.
*/
