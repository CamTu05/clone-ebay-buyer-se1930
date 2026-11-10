using System;
using System.Collections.Generic;
using Buyer.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Infrastructure.Data;

public partial class CloneEbayDbContext : DbContext
{
    public CloneEbayDbContext(DbContextOptions<CloneEbayDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Auction> Auctions { get; set; }

    public virtual DbSet<Bid> Bids { get; set; }

    public virtual DbSet<CancellationRequest> CancellationRequests { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<CheckoutGroup> CheckoutGroups { get; set; }

    public virtual DbSet<Coupon> Coupons { get; set; }

    public virtual DbSet<CouponUsage> CouponUsages { get; set; }

    public virtual DbSet<ExternalLogin> ExternalLogins { get; set; }

    public virtual DbSet<GuestCheckoutAccess> GuestCheckoutAccesses { get; set; }

    public virtual DbSet<Listing> Listings { get; set; }

    public virtual DbSet<ListingImage> ListingImages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderTable> OrderTables { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ReturnRequest> ReturnRequests { get; set; }

    public virtual DbSet<ReturnRequestItem> ReturnRequestItems { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<SellerFeedback> SellerFeedbacks { get; set; }

    public virtual DbSet<ShippingInfo> ShippingInfos { get; set; }

    public virtual DbSet<ShippingOption> ShippingOptions { get; set; }

    public virtual DbSet<ShippingPolicy> ShippingPolicies { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VerificationToken> VerificationTokens { get; set; }

    public virtual DbSet<Watchlist> Watchlists { get; set; }

    public virtual DbSet<vw_ActiveListing> vw_ActiveListings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.ToTable("Address");

            entity.HasIndex(e => e.userId, "IX_Address_User");

            entity.HasIndex(e => new { e.userId, e.isDefault }, "UX_Address_OneDefaultPerUser")
                .IsUnique()
                .HasFilter("([isDefault]=(1))");

            entity.Property(e => e.addressLine2).HasMaxLength(255);
            entity.Property(e => e.city).HasMaxLength(100);
            entity.Property(e => e.country).HasMaxLength(100);
            entity.Property(e => e.countryCode).HasMaxLength(10);
            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.fullName).HasMaxLength(150);
            entity.Property(e => e.phone).HasMaxLength(30);
            entity.Property(e => e.postalCode).HasMaxLength(30);
            entity.Property(e => e.state).HasMaxLength(100);
            entity.Property(e => e.street).HasMaxLength(255);
            entity.Property(e => e.updatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.user).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.userId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Address_User");
        });

        modelBuilder.Entity<Auction>(entity =>
        {
            entity.ToTable("Auction");

            entity.HasIndex(e => new { e.status, e.endTime }, "IX_Auction_StatusEndTime");

            entity.HasIndex(e => e.listingId, "UQ_Auction_Listing").IsUnique();

            entity.Property(e => e.bidIncrement).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.currentPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.endTime).HasPrecision(3);
            entity.Property(e => e.resolvedAt).HasPrecision(3);
            entity.Property(e => e.rowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.startTime).HasPrecision(3);
            entity.Property(e => e.startingPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("SCHEDULED");
            entity.Property(e => e.winningPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.highestBidder).WithMany(p => p.AuctionhighestBidders)
                .HasForeignKey(d => d.highestBidderId)
                .HasConstraintName("FK_Auction_HighestBidder");

            entity.HasOne(d => d.listing).WithOne(p => p.Auction)
                .HasForeignKey<Auction>(d => d.listingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Auction_Listing");

            entity.HasOne(d => d.winner).WithMany(p => p.Auctionwinners)
                .HasForeignKey(d => d.winnerId)
                .HasConstraintName("FK_Auction_Winner");
        });

        modelBuilder.Entity<Bid>(entity =>
        {
            entity.ToTable("Bid");

            entity.HasIndex(e => new { e.auctionId, e.maximumBid, e.createdAt, e.id }, "IX_Bid_AuctionMaximum").IsDescending(false, true, false, false);

            entity.HasIndex(e => new { e.bidderId, e.createdAt }, "IX_Bid_BidderCreated").IsDescending(false, true);

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.effectiveBid).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.maximumBid).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE");

            entity.HasOne(d => d.auction).WithMany(p => p.Bids)
                .HasForeignKey(d => d.auctionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bid_Auction");

            entity.HasOne(d => d.bidder).WithMany(p => p.Bids)
                .HasForeignKey(d => d.bidderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bid_Bidder");
        });

        modelBuilder.Entity<CancellationRequest>(entity =>
        {
            entity.ToTable("CancellationRequest");

            entity.HasIndex(e => new { e.requestedBy, e.createdAt }, "IX_CancellationRequest_RequestedBy").IsDescending(false, true);

            entity.HasIndex(e => new { e.orderId, e.status }, "UX_CancellationRequest_OnePendingPerOrder")
                .IsUnique()
                .HasFilter("([status]=N'PENDING')");

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.reason).HasMaxLength(1000);
            entity.Property(e => e.resolvedAt).HasPrecision(3);
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");

            entity.HasOne(d => d.order).WithMany(p => p.CancellationRequests)
                .HasForeignKey(d => d.orderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CancellationRequest_Order");

            entity.HasOne(d => d.requestedByNavigation).WithMany(p => p.CancellationRequests)
                .HasForeignKey(d => d.requestedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CancellationRequest_User");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Cart");

            entity.HasIndex(e => e.userId, "UQ_Cart_User").IsUnique();

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.updatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.user).WithOne(p => p.Cart)
                .HasForeignKey<Cart>(d => d.userId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cart_User");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItem");

            entity.HasIndex(e => new { e.cartId, e.status }, "IX_CartItem_CartStatus");

            entity.HasIndex(e => new { e.cartId, e.listingId }, "UQ_CartItem_CartListing").IsUnique();

            entity.Property(e => e.addedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE");

            entity.HasOne(d => d.cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.cartId)
                .HasConstraintName("FK_CartItem_Cart");

            entity.HasOne(d => d.listing).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.listingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CartItem_Listing");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category");

            entity.HasIndex(e => e.parentCategoryId, "IX_Category_Parent");

            entity.HasIndex(e => e.slug, "UQ_Category_Slug").IsUnique();

            entity.Property(e => e.isActive).HasDefaultValue(true);
            entity.Property(e => e.name).HasMaxLength(150);
            entity.Property(e => e.slug).HasMaxLength(180);

            entity.HasOne(d => d.parentCategory).WithMany(p => p.InverseparentCategory)
                .HasForeignKey(d => d.parentCategoryId)
                .HasConstraintName("FK_Category_Parent");
        });

        modelBuilder.Entity<CheckoutGroup>(entity =>
        {
            entity.ToTable("CheckoutGroup");

            entity.HasIndex(e => new { e.buyerId, e.createdAt }, "IX_CheckoutGroup_BuyerCreated")
                .IsDescending(false, true)
                .HasFilter("([buyerId] IS NOT NULL)");

            entity.HasIndex(e => new { e.status, e.paymentDeadline }, "IX_CheckoutGroup_Expiry").HasFilter("([paymentDeadline] IS NOT NULL)");

            entity.HasIndex(e => e.guestEmail, "IX_CheckoutGroup_GuestEmail").HasFilter("([guestEmail] IS NOT NULL)");

            entity.Property(e => e.confirmedAt).HasPrecision(3);
            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.currency)
                .HasMaxLength(10)
                .HasDefaultValue("VND");
            entity.Property(e => e.discountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.guestEmail).HasMaxLength(255);
            entity.Property(e => e.inventoryReleasedAt).HasPrecision(3);
            entity.Property(e => e.paymentDeadline).HasPrecision(3);
            entity.Property(e => e.shippingFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.totalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.buyer).WithMany(p => p.CheckoutGroups)
                .HasForeignKey(d => d.buyerId)
                .HasConstraintName("FK_CheckoutGroup_Buyer");

            entity.HasOne(d => d.coupon).WithMany(p => p.CheckoutGroups)
                .HasForeignKey(d => d.couponId)
                .HasConstraintName("FK_CheckoutGroup_Coupon");
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.ToTable("Coupon");

            entity.HasIndex(e => e.code, "UQ_Coupon_Code").IsUnique();

            entity.Property(e => e.code).HasMaxLength(50);
            entity.Property(e => e.discountType).HasMaxLength(20);
            entity.Property(e => e.discountValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.endDate).HasPrecision(3);
            entity.Property(e => e.isActive).HasDefaultValue(true);
            entity.Property(e => e.maxDiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.minOrderValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.startDate).HasPrecision(3);
        });

        modelBuilder.Entity<CouponUsage>(entity =>
        {
            entity.ToTable("CouponUsage");

            entity.HasIndex(e => new { e.couponId, e.userId }, "IX_CouponUsage_CouponUser");

            entity.HasIndex(e => e.checkoutGroupId, "UQ_CouponUsage_Group").IsUnique();

            entity.Property(e => e.discountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.usedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.checkoutGroup).WithOne(p => p.CouponUsage)
                .HasForeignKey<CouponUsage>(d => d.checkoutGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CouponUsage_Group");

            entity.HasOne(d => d.coupon).WithMany(p => p.CouponUsages)
                .HasForeignKey(d => d.couponId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CouponUsage_Coupon");

            entity.HasOne(d => d.user).WithMany(p => p.CouponUsages)
                .HasForeignKey(d => d.userId)
                .HasConstraintName("FK_CouponUsage_User");
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.ToTable("ExternalLogin");

            entity.HasIndex(e => new { e.provider, e.providerUserId }, "UQ_ExternalLogin_ProviderIdentity").IsUnique();

            entity.HasIndex(e => new { e.userId, e.provider }, "UQ_ExternalLogin_UserProvider").IsUnique();

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.provider).HasMaxLength(20);
            entity.Property(e => e.providerUserId).HasMaxLength(255);

            entity.HasOne(d => d.user).WithMany(p => p.ExternalLogins)
                .HasForeignKey(d => d.userId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExternalLogin_User");
        });

        modelBuilder.Entity<GuestCheckoutAccess>(entity =>
        {
            entity.ToTable("GuestCheckoutAccess");

            entity.HasIndex(e => new { e.checkoutGroupId, e.expiresAt }, "IX_GuestCheckoutAccess_Group");

            entity.HasIndex(e => e.tokenHash, "UQ_GuestCheckoutAccess_TokenHash").IsUnique();

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.expiresAt).HasPrecision(3);
            entity.Property(e => e.revokedAt).HasPrecision(3);
            entity.Property(e => e.tokenHash).HasMaxLength(500);

            entity.HasOne(d => d.checkoutGroup).WithMany(p => p.GuestCheckoutAccesses)
                .HasForeignKey(d => d.checkoutGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GuestCheckoutAccess_Group");
        });

        modelBuilder.Entity<Listing>(entity =>
        {
            entity.ToTable("Listing");

            entity.HasIndex(e => new { e.categoryId, e.status, e.listingType }, "IX_Listing_CategoryStatus");

            entity.HasIndex(e => e.productId, "IX_Listing_Product").HasFilter("([productId] IS NOT NULL)");

            entity.HasIndex(e => new { e.sellerId, e.status }, "IX_Listing_SellerStatus");

            entity.HasIndex(e => new { e.status, e.listingType, e.price }, "IX_Listing_StatusPrice");

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.endedAt).HasPrecision(3);
            entity.Property(e => e.itemCondition).HasMaxLength(50);
            entity.Property(e => e.listingType).HasMaxLength(20);
            entity.Property(e => e.price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.publishedAt).HasPrecision(3);
            entity.Property(e => e.quantity).HasDefaultValue(1);
            entity.Property(e => e.rowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("DRAFT");
            entity.Property(e => e.title).HasMaxLength(255);

            entity.HasOne(d => d.category).WithMany(p => p.Listings)
                .HasForeignKey(d => d.categoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Listing_Category");

            entity.HasOne(d => d.product).WithMany(p => p.Listings)
                .HasForeignKey(d => d.productId)
                .HasConstraintName("FK_Listing_Product");

            entity.HasOne(d => d.seller).WithMany(p => p.Listings)
                .HasForeignKey(d => d.sellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Listing_Seller");

            entity.HasOne(d => d.ShippingPolicy).WithMany(p => p.Listings)
                .HasPrincipalKey(p => new { p.id, p.sellerId })
                .HasForeignKey(d => new { d.shippingPolicyId, d.sellerId })
                .HasConstraintName("FK_Listing_ShippingPolicySeller");
        });

        modelBuilder.Entity<ListingImage>(entity =>
        {
            entity.ToTable("ListingImage");

            entity.HasIndex(e => new { e.listingId, e.sortOrder }, "IX_ListingImage_ListingOrder");

            entity.HasIndex(e => new { e.listingId, e.isPrimary }, "UX_ListingImage_OnePrimary")
                .IsUnique()
                .HasFilter("([isPrimary]=(1))");

            entity.Property(e => e.imageUrl).HasMaxLength(1000);

            entity.HasOne(d => d.listing).WithMany(p => p.ListingImages)
                .HasForeignKey(d => d.listingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ListingImage_Listing");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notification");

            entity.HasIndex(e => new { e.userId, e.isRead, e.createdAt }, "IX_Notification_UserUnread").IsDescending(false, false, true);

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.readAt).HasPrecision(3);
            entity.Property(e => e.referenceId).HasMaxLength(100);
            entity.Property(e => e.referenceType).HasMaxLength(50);
            entity.Property(e => e.title).HasMaxLength(255);
            entity.Property(e => e.type).HasMaxLength(40);

            entity.HasOne(d => d.user).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.userId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notification_User");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItem");

            entity.HasIndex(e => e.listingId, "IX_OrderItem_Listing");

            entity.HasIndex(e => e.orderId, "IX_OrderItem_Order");

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.imageUrl).HasMaxLength(1000);
            entity.Property(e => e.productTitle).HasMaxLength(255);
            entity.Property(e => e.unitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.listing).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.listingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItem_Listing");

            entity.HasOne(d => d.order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.orderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItem_Order");

            entity.HasOne(d => d.product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.productId)
                .HasConstraintName("FK_OrderItem_Product");
        });

        modelBuilder.Entity<OrderTable>(entity =>
        {
            entity.ToTable("OrderTable");

            entity.HasIndex(e => new { e.buyerId, e.orderDate }, "IX_Order_BuyerCreated")
                .IsDescending(false, true)
                .HasFilter("([buyerId] IS NOT NULL)");

            entity.HasIndex(e => e.checkoutGroupId, "IX_Order_CheckoutGroup");

            entity.HasIndex(e => new { e.sellerId, e.status, e.orderDate }, "IX_Order_SellerStatus").IsDescending(false, false, true);

            entity.Property(e => e.currency)
                .HasMaxLength(10)
                .HasDefaultValue("VND");
            entity.Property(e => e.discountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.orderDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.shippingAddressLine2).HasMaxLength(255);
            entity.Property(e => e.shippingCity).HasMaxLength(100);
            entity.Property(e => e.shippingCountry).HasMaxLength(100);
            entity.Property(e => e.shippingCountryCode).HasMaxLength(10);
            entity.Property(e => e.shippingEmail).HasMaxLength(255);
            entity.Property(e => e.shippingFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.shippingFullName).HasMaxLength(150);
            entity.Property(e => e.shippingPhone).HasMaxLength(30);
            entity.Property(e => e.shippingPostalCode).HasMaxLength(30);
            entity.Property(e => e.shippingServiceName).HasMaxLength(150);
            entity.Property(e => e.shippingState).HasMaxLength(100);
            entity.Property(e => e.shippingStreet).HasMaxLength(255);
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("PLACED");
            entity.Property(e => e.stockRestoredAt).HasPrecision(3);
            entity.Property(e => e.subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.totalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.updatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.address).WithMany(p => p.OrderTables)
                .HasForeignKey(d => d.addressId)
                .HasConstraintName("FK_Order_Address");

            entity.HasOne(d => d.buyer).WithMany(p => p.OrderTablebuyers)
                .HasForeignKey(d => d.buyerId)
                .HasConstraintName("FK_Order_Buyer");

            entity.HasOne(d => d.checkoutGroup).WithMany(p => p.OrderTables)
                .HasForeignKey(d => d.checkoutGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Order_CheckoutGroup");

            entity.HasOne(d => d.seller).WithMany(p => p.OrderTablesellers)
                .HasForeignKey(d => d.sellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Order_Seller");

            entity.HasOne(d => d.shippingOption).WithMany(p => p.OrderTables)
                .HasForeignKey(d => d.shippingOptionId)
                .HasConstraintName("FK_Order_ShippingOption");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payment");

            entity.HasIndex(e => new { e.checkoutGroupId, e.createdAt }, "IX_Payment_GroupCreated").IsDescending(false, true);

            entity.HasIndex(e => new { e.checkoutGroupId, e.method }, "UX_Payment_OneCODPerCheckout")
                .IsUnique()
                .HasFilter("([method]=N'COD')");

            entity.HasIndex(e => new { e.checkoutGroupId, e.status }, "UX_Payment_OnePaidPerCheckout")
                .IsUnique()
                .HasFilter("([status]=N'PAID')");

            entity.HasIndex(e => e.transactionId, "UX_Payment_TransactionId")
                .IsUnique()
                .HasFilter("([transactionId] IS NOT NULL)");

            entity.Property(e => e.amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.failureReason).HasMaxLength(500);
            entity.Property(e => e.method).HasMaxLength(20);
            entity.Property(e => e.paidAt).HasPrecision(3);
            entity.Property(e => e.provider).HasMaxLength(50);
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.transactionId).HasMaxLength(150);

            entity.HasOne(d => d.checkoutGroup).WithMany(p => p.Payments)
                .HasForeignKey(d => d.checkoutGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_CheckoutGroup");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Product");

            entity.HasIndex(e => e.categoryId, "IX_Product_Category");

            entity.HasIndex(e => e.title, "IX_Product_Title");

            entity.Property(e => e.brand).HasMaxLength(150);
            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.title).HasMaxLength(255);
            entity.Property(e => e.updatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.category).WithMany(p => p.Products)
                .HasForeignKey(d => d.categoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Product_Category");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("ProductImage");

            entity.HasIndex(e => new { e.productId, e.sortOrder }, "IX_ProductImage_ProductOrder");

            entity.HasIndex(e => new { e.productId, e.isPrimary }, "UX_ProductImage_OnePrimary")
                .IsUnique()
                .HasFilter("([isPrimary]=(1))");

            entity.Property(e => e.imageUrl).HasMaxLength(1000);

            entity.HasOne(d => d.product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.productId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductImage_Product");
        });

        modelBuilder.Entity<ReturnRequest>(entity =>
        {
            entity.ToTable("ReturnRequest");

            entity.HasIndex(e => new { e.orderId, e.createdAt }, "IX_ReturnRequest_OrderCreated").IsDescending(false, true);

            entity.HasIndex(e => new { e.requestedBy, e.createdAt }, "IX_ReturnRequest_RequestedBy").IsDescending(false, true);

            entity.Property(e => e.approvedAt).HasPrecision(3);
            entity.Property(e => e.completedAt).HasPrecision(3);
            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.reasonCode).HasMaxLength(50);
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("REQUESTED");

            entity.HasOne(d => d.order).WithMany(p => p.ReturnRequests)
                .HasForeignKey(d => d.orderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReturnRequest_Order");

            entity.HasOne(d => d.requestedByNavigation).WithMany(p => p.ReturnRequests)
                .HasForeignKey(d => d.requestedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReturnRequest_User");
        });

        modelBuilder.Entity<ReturnRequestItem>(entity =>
        {
            entity.ToTable("ReturnRequestItem");

            entity.HasIndex(e => e.orderItemId, "IX_ReturnRequestItem_OrderItem");

            entity.HasIndex(e => new { e.returnRequestId, e.orderItemId }, "UQ_ReturnRequestItem_RequestItem").IsUnique();

            entity.HasOne(d => d.orderItem).WithMany(p => p.ReturnRequestItems)
                .HasForeignKey(d => d.orderItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReturnRequestItem_OrderItem");

            entity.HasOne(d => d.returnRequest).WithMany(p => p.ReturnRequestItems)
                .HasForeignKey(d => d.returnRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReturnRequestItem_Request");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("Review");

            entity.HasIndex(e => new { e.productId, e.createdAt }, "IX_Review_ProductCreated").IsDescending(false, true);

            entity.HasIndex(e => e.orderItemId, "UQ_Review_OrderItem").IsUnique();

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.orderItem).WithOne(p => p.Review)
                .HasForeignKey<Review>(d => d.orderItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Review_OrderItem");

            entity.HasOne(d => d.product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.productId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Review_Product");

            entity.HasOne(d => d.reviewer).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.reviewerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Review_User");
        });

        modelBuilder.Entity<SellerFeedback>(entity =>
        {
            entity.ToTable("SellerFeedback");

            entity.HasIndex(e => new { e.sellerId, e.createdAt }, "IX_SellerFeedback_SellerCreated").IsDescending(false, true);

            entity.HasIndex(e => e.orderId, "UQ_SellerFeedback_Order").IsUnique();

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.feedbackType).HasMaxLength(20);

            entity.HasOne(d => d.buyer).WithMany(p => p.SellerFeedbackbuyers)
                .HasForeignKey(d => d.buyerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SellerFeedback_Buyer");

            entity.HasOne(d => d.order).WithOne(p => p.SellerFeedback)
                .HasForeignKey<SellerFeedback>(d => d.orderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SellerFeedback_Order");

            entity.HasOne(d => d.seller).WithMany(p => p.SellerFeedbacksellers)
                .HasForeignKey(d => d.sellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SellerFeedback_Seller");
        });

        modelBuilder.Entity<ShippingInfo>(entity =>
        {
            entity.ToTable("ShippingInfo");

            entity.HasIndex(e => e.trackingNumber, "IX_ShippingInfo_Tracking").HasFilter("([trackingNumber] IS NOT NULL)");

            entity.HasIndex(e => e.orderId, "UQ_ShippingInfo_Order").IsUnique();

            entity.Property(e => e.carrier).HasMaxLength(100);
            entity.Property(e => e.deliveredAt).HasPrecision(3);
            entity.Property(e => e.estimatedArrival).HasPrecision(3);
            entity.Property(e => e.shippedAt).HasPrecision(3);
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.trackingNumber).HasMaxLength(150);
            entity.Property(e => e.updatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.order).WithOne(p => p.ShippingInfo)
                .HasForeignKey<ShippingInfo>(d => d.orderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShippingInfo_Order");
        });

        modelBuilder.Entity<ShippingOption>(entity =>
        {
            entity.ToTable("ShippingOption");

            entity.HasIndex(e => e.shippingPolicyId, "IX_ShippingOption_Policy");

            entity.HasIndex(e => new { e.id, e.shippingPolicyId }, "UQ_ShippingOption_IdPolicy").IsUnique();

            entity.Property(e => e.flatRate).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.isActive).HasDefaultValue(true);
            entity.Property(e => e.serviceName).HasMaxLength(150);
            entity.Property(e => e.serviceType).HasMaxLength(20);

            entity.HasOne(d => d.shippingPolicy).WithMany(p => p.ShippingOptions)
                .HasForeignKey(d => d.shippingPolicyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShippingOption_Policy");
        });

        modelBuilder.Entity<ShippingPolicy>(entity =>
        {
            entity.ToTable("ShippingPolicy");

            entity.HasIndex(e => e.sellerId, "IX_ShippingPolicy_Seller");

            entity.HasIndex(e => new { e.id, e.sellerId }, "UQ_ShippingPolicy_IdSeller").IsUnique();

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.isActive).HasDefaultValue(true);
            entity.Property(e => e.name).HasMaxLength(150);

            entity.HasOne(d => d.seller).WithMany(p => p.ShippingPolicies)
                .HasForeignKey(d => d.sellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShippingPolicy_Seller");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.HasIndex(e => e.username, "IX_User_Username");

            entity.HasIndex(e => e.email, "UQ_User_Email").IsUnique();

            entity.Property(e => e.accountStatus)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE");
            entity.Property(e => e.avatarURL).HasMaxLength(1000);
            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.email).HasMaxLength(255);
            entity.Property(e => e.lastLoginAt).HasPrecision(3);
            entity.Property(e => e.passwordHash).HasMaxLength(500);
            entity.Property(e => e.phoneNumber).HasMaxLength(30);
            entity.Property(e => e.role)
                .HasMaxLength(20)
                .HasDefaultValue("BUYER");
            entity.Property(e => e.updatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.username).HasMaxLength(100);
        });

        modelBuilder.Entity<VerificationToken>(entity =>
        {
            entity.ToTable("VerificationToken");

            entity.HasIndex(e => new { e.userId, e.tokenType, e.expiresAt }, "IX_VerificationToken_UserType");

            entity.HasIndex(e => e.tokenHash, "UQ_VerificationToken_Hash").IsUnique();

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.expiresAt).HasPrecision(3);
            entity.Property(e => e.tokenHash).HasMaxLength(500);
            entity.Property(e => e.tokenType).HasMaxLength(30);
            entity.Property(e => e.usedAt).HasPrecision(3);

            entity.HasOne(d => d.user).WithMany(p => p.VerificationTokens)
                .HasForeignKey(d => d.userId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VerificationToken_User");
        });

        modelBuilder.Entity<Watchlist>(entity =>
        {
            entity.ToTable("Watchlist");

            entity.HasIndex(e => e.listingId, "IX_Watchlist_Listing");

            entity.HasIndex(e => new { e.userId, e.listingId }, "UQ_Watchlist_UserListing").IsUnique();

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.listing).WithMany(p => p.Watchlists)
                .HasForeignKey(d => d.listingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Watchlist_Listing");

            entity.HasOne(d => d.user).WithMany(p => p.Watchlists)
                .HasForeignKey(d => d.userId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Watchlist_User");
        });

        modelBuilder.Entity<vw_ActiveListing>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ActiveListings");

            entity.Property(e => e.auctionEndTime).HasPrecision(3);
            entity.Property(e => e.auctionStartTime).HasPrecision(3);
            entity.Property(e => e.catalogBrand).HasMaxLength(150);
            entity.Property(e => e.catalogProductTitle).HasMaxLength(255);
            entity.Property(e => e.currentPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.itemCondition).HasMaxLength(50);
            entity.Property(e => e.listingType).HasMaxLength(20);
            entity.Property(e => e.price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.publishedAt).HasPrecision(3);
            entity.Property(e => e.status).HasMaxLength(20);
            entity.Property(e => e.title).HasMaxLength(255);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
