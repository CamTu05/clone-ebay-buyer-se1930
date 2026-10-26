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

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<CheckoutGroup> CheckoutGroups { get; set; }

    public virtual DbSet<Coupon> Coupons { get; set; }

    public virtual DbSet<CouponUsage> CouponUsages { get; set; }

    public virtual DbSet<Dispute> Disputes { get; set; }

    public virtual DbSet<ExternalLogin> ExternalLogins { get; set; }

    public virtual DbSet<GiftCard> GiftCards { get; set; }

    public virtual DbSet<GiftCardTransaction> GiftCardTransactions { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<Listing> Listings { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderTable> OrderTables { get; set; }

    public virtual DbSet<OtpChallenge> OtpChallenges { get; set; }

    public virtual DbSet<PackageInfo> PackageInfos { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ReturnRequest> ReturnRequests { get; set; }

    public virtual DbSet<ReturnRequestItem> ReturnRequestItems { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<SellerFeedback> SellerFeedbacks { get; set; }

    public virtual DbSet<SellerFeedbackSummary> SellerFeedbackSummaries { get; set; }

    public virtual DbSet<ShippingInfo> ShippingInfos { get; set; }

    public virtual DbSet<ShippingOption> ShippingOptions { get; set; }

    public virtual DbSet<ShippingPolicy> ShippingPolicies { get; set; }

    public virtual DbSet<ShippingRegion> ShippingRegions { get; set; }

    public virtual DbSet<Store> Stores { get; set; }

    public virtual DbSet<TaxRegion> TaxRegions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VerificationToken> VerificationTokens { get; set; }

    public virtual DbSet<Watchlist> Watchlists { get; set; }

    public virtual DbSet<vw_ActiveListing> vw_ActiveListings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Address__3213E83F83ABAF10");

            entity.ToTable("Address");

            entity.HasIndex(e => e.userId, "IX_Address_User");

            entity.HasIndex(e => e.userId, "UX_Address_OneDefaultPerUser")
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
            entity.HasKey(e => e.id).HasName("PK__Auction__3213E83FEC979D95");

            entity.ToTable("Auction");

            entity.HasIndex(e => new { e.status, e.endTime }, "IX_Auction_StatusEndTime");

            entity.HasIndex(e => e.listingId, "UQ__Auction__5A0F3D9898B36B6F").IsUnique();

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
            entity.HasKey(e => e.id).HasName("PK__Bid__3213E83FFA20272A");

            entity.ToTable("Bid");

            entity.HasIndex(e => new { e.auctionId, e.createdAt, e.id }, "IX_Bid_AuctionCreatedAt");

            entity.HasIndex(e => new { e.auctionId, e.maximumBid, e.createdAt }, "IX_Bid_AuctionMaximum").IsDescending(false, true, false);

            entity.HasIndex(e => new { e.bidderId, e.createdAt }, "IX_Bid_Bidder").IsDescending(false, true);

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

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Cart__3213E83F83826705");

            entity.ToTable("Cart");

            entity.HasIndex(e => e.userId, "UQ__Cart__CB9A1CFE689E260E").IsUnique();

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
            entity.HasKey(e => e.id).HasName("PK__CartItem__3213E83FE281038B");

            entity.ToTable("CartItem");

            entity.HasIndex(e => new { e.cartId, e.listingId }, "UQ_CartItem").IsUnique();

            entity.Property(e => e.addedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

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
            entity.HasKey(e => e.id).HasName("PK__Category__3213E83F771D2DA6");

            entity.ToTable("Category");

            entity.HasIndex(e => e.slug, "UQ__Category__32DD1E4C22EA0172").IsUnique();

            entity.Property(e => e.isActive).HasDefaultValue(true);
            entity.Property(e => e.name).HasMaxLength(150);
            entity.Property(e => e.slug).HasMaxLength(180);

            entity.HasOne(d => d.parentCategory).WithMany(p => p.InverseparentCategory)
                .HasForeignKey(d => d.parentCategoryId)
                .HasConstraintName("FK_Category_Parent");
        });

        modelBuilder.Entity<CheckoutGroup>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Checkout__3213E83F5587B367");

            entity.ToTable("CheckoutGroup");

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.currency)
                .HasMaxLength(10)
                .HasDefaultValue("VND");
            entity.Property(e => e.discountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.guestEmail).HasMaxLength(255);
            entity.Property(e => e.importCharge).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.shippingFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.taxAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.totalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.buyer).WithMany(p => p.CheckoutGroups)
                .HasForeignKey(d => d.buyerId)
                .HasConstraintName("FK_CheckoutGroup_Buyer");
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Coupon__3213E83F3D5ABB19");

            entity.ToTable("Coupon");

            entity.HasIndex(e => e.code, "UQ__Coupon__357D4CF917A40444").IsUnique();

            entity.Property(e => e.code).HasMaxLength(50);
            entity.Property(e => e.discountType).HasMaxLength(20);
            entity.Property(e => e.discountValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.endDate).HasPrecision(3);
            entity.Property(e => e.isActive).HasDefaultValue(true);
            entity.Property(e => e.maxDiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.minOrderValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.startDate).HasPrecision(3);

            entity.HasOne(d => d.seller).WithMany(p => p.Coupons)
                .HasForeignKey(d => d.sellerId)
                .HasConstraintName("FK_Coupon_Seller");

            entity.HasMany(d => d.listings).WithMany(p => p.coupons)
                .UsingEntity<Dictionary<string, object>>(
                    "CouponListing",
                    r => r.HasOne<Listing>().WithMany()
                        .HasForeignKey("listingId")
                        .HasConstraintName("FK_CouponListing_Listing"),
                    l => l.HasOne<Coupon>().WithMany()
                        .HasForeignKey("couponId")
                        .HasConstraintName("FK_CouponListing_Coupon"),
                    j =>
                    {
                        j.HasKey("couponId", "listingId");
                        j.ToTable("CouponListing");
                    });
        });

        modelBuilder.Entity<CouponUsage>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__CouponUs__3213E83F7DAED181");

            entity.ToTable("CouponUsage");

            entity.HasIndex(e => new { e.couponId, e.userId }, "IX_CouponUsage_CouponUser");

            entity.Property(e => e.discountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.usedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.coupon).WithMany(p => p.CouponUsages)
                .HasForeignKey(d => d.couponId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CouponUsage_Coupon");

            entity.HasOne(d => d.order).WithMany(p => p.CouponUsages)
                .HasForeignKey(d => d.orderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CouponUsage_Order");

            entity.HasOne(d => d.user).WithMany(p => p.CouponUsages)
                .HasForeignKey(d => d.userId)
                .HasConstraintName("FK_CouponUsage_User");
        });

        modelBuilder.Entity<Dispute>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Dispute__3213E83F07454F01");

            entity.ToTable("Dispute");

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.resolvedAt).HasPrecision(3);
            entity.Property(e => e.status)
                .HasMaxLength(30)
                .HasDefaultValue("OPEN");

            entity.HasOne(d => d.order).WithMany(p => p.Disputes)
                .HasForeignKey(d => d.orderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dispute_Order");

            entity.HasOne(d => d.raisedByNavigation).WithMany(p => p.Disputes)
                .HasForeignKey(d => d.raisedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dispute_User");
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__External__3213E83F875DEFC7");

            entity.ToTable("ExternalLogin");

            entity.HasIndex(e => new { e.provider, e.providerUserId }, "UQ_ExternalLogin_ProviderUser").IsUnique();

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.provider).HasMaxLength(50);
            entity.Property(e => e.providerUserId).HasMaxLength(255);

            entity.HasOne(d => d.user).WithMany(p => p.ExternalLogins)
                .HasForeignKey(d => d.userId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExternalLogin_User");
        });

        modelBuilder.Entity<GiftCard>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__GiftCard__3213E83F2975A481");

            entity.ToTable("GiftCard");

            entity.HasIndex(e => e.codeHash, "UQ__GiftCard__09A5C64795F1124B").IsUnique();

            entity.Property(e => e.codeHash).HasMaxLength(500);
            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.currentBalance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.expiresAt).HasPrecision(3);
            entity.Property(e => e.initialBalance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE");

            entity.HasOne(d => d.linkedUser).WithMany(p => p.GiftCards)
                .HasForeignKey(d => d.linkedUserId)
                .HasConstraintName("FK_GiftCard_User");
        });

        modelBuilder.Entity<GiftCardTransaction>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__GiftCard__3213E83F9DC3593F");

            entity.ToTable("GiftCardTransaction");

            entity.Property(e => e.amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.transactionType).HasMaxLength(20);

            entity.HasOne(d => d.giftCard).WithMany(p => p.GiftCardTransactions)
                .HasForeignKey(d => d.giftCardId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GiftCardTransaction_Card");

            entity.HasOne(d => d.order).WithMany(p => p.GiftCardTransactions)
                .HasForeignKey(d => d.orderId)
                .HasConstraintName("FK_GiftCardTransaction_Order");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Inventor__3213E83F88A7E933");

            entity.ToTable("Inventory");

            entity.HasIndex(e => e.listingId, "UQ__Inventor__5A0F3D982DD36B0E").IsUnique();

            entity.Property(e => e.lastUpdated)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.rowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.listing).WithOne(p => p.Inventory)
                .HasForeignKey<Inventory>(d => d.listingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inventory_Listing");
        });

        modelBuilder.Entity<Listing>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Listing__3213E83FEC19B6A5");

            entity.ToTable("Listing");

            entity.HasIndex(e => e.productId, "IX_Listing_Product");

            entity.HasIndex(e => new { e.sellerId, e.status }, "IX_Listing_SellerStatus");

            entity.HasIndex(e => new { e.listingType, e.status }, "IX_Listing_TypeStatus");

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.endedAt).HasPrecision(3);
            entity.Property(e => e.listingType).HasMaxLength(20);
            entity.Property(e => e.price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.publishedAt).HasPrecision(3);
            entity.Property(e => e.quantity).HasDefaultValue(1);
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("DRAFT");

            entity.HasOne(d => d.product).WithMany(p => p.Listings)
                .HasForeignKey(d => d.productId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Listing_Product");

            entity.HasOne(d => d.seller).WithMany(p => p.Listings)
                .HasForeignKey(d => d.sellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Listing_Seller");

            entity.HasOne(d => d.shippingPolicy).WithMany(p => p.Listings)
                .HasForeignKey(d => d.shippingPolicyId)
                .HasConstraintName("FK_Listing_ShippingPolicy");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Message__3213E83F0A815D04");

            entity.ToTable("Message");

            entity.HasIndex(e => new { e.senderId, e.receiverId, e.sentAt }, "IX_Message_Conversation");

            entity.Property(e => e.sentAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.receiver).WithMany(p => p.Messagereceivers)
                .HasForeignKey(d => d.receiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Message_Receiver");

            entity.HasOne(d => d.sender).WithMany(p => p.Messagesenders)
                .HasForeignKey(d => d.senderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Message_Sender");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Notifica__3213E83F758905C5");

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
            entity.HasKey(e => e.id).HasName("PK__OrderIte__3213E83F9F4B60CC");

            entity.ToTable("OrderItem");

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
                .HasConstraintName("FK_OrderItem_Order");

            entity.HasOne(d => d.product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.productId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItem_Product");

            entity.HasOne(d => d.seller).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.sellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItem_Seller");
        });

        modelBuilder.Entity<OrderTable>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__OrderTab__3213E83F266AA0AC");

            entity.ToTable("OrderTable");

            entity.HasIndex(e => new { e.buyerId, e.createdAt }, "IX_Order_BuyerCreatedAt").IsDescending(false, true);

            entity.HasIndex(e => e.checkoutGroupId, "IX_Order_CheckoutGroup");

            entity.HasIndex(e => new { e.sellerId, e.status, e.createdAt }, "IX_Order_SellerStatus").IsDescending(false, false, true);

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.currency)
                .HasMaxLength(10)
                .HasDefaultValue("VND");
            entity.Property(e => e.discountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.guestEmail).HasMaxLength(255);
            entity.Property(e => e.importCharge).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.orderDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.paymentDeadline).HasPrecision(3);
            entity.Property(e => e.shippingAddressLine2).HasMaxLength(255);
            entity.Property(e => e.shippingCity).HasMaxLength(100);
            entity.Property(e => e.shippingCountry).HasMaxLength(100);
            entity.Property(e => e.shippingCountryCode).HasMaxLength(10);
            entity.Property(e => e.shippingFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.shippingFullName).HasMaxLength(150);
            entity.Property(e => e.shippingPhone).HasMaxLength(30);
            entity.Property(e => e.shippingPostalCode).HasMaxLength(30);
            entity.Property(e => e.shippingServiceName).HasMaxLength(150);
            entity.Property(e => e.shippingServiceType).HasMaxLength(20);
            entity.Property(e => e.shippingState).HasMaxLength(100);
            entity.Property(e => e.shippingStreet).HasMaxLength(255);
            entity.Property(e => e.status)
                .HasMaxLength(30)
                .HasDefaultValue("PENDING_PAYMENT");
            entity.Property(e => e.subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.taxAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.taxRateSnapshot).HasColumnType("decimal(7, 4)");
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
                .HasConstraintName("FK_Order_CheckoutGroup");

            entity.HasOne(d => d.coupon).WithMany(p => p.OrderTables)
                .HasForeignKey(d => d.couponId)
                .HasConstraintName("FK_Order_Coupon");

            entity.HasOne(d => d.seller).WithMany(p => p.OrderTablesellers)
                .HasForeignKey(d => d.sellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Order_Seller");

            entity.HasOne(d => d.shippingOption).WithMany(p => p.OrderTables)
                .HasForeignKey(d => d.shippingOptionId)
                .HasConstraintName("FK_Order_ShippingOption");
        });

        modelBuilder.Entity<OtpChallenge>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__OtpChall__3213E83FA728C714");

            entity.ToTable("OtpChallenge");

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.expiresAt).HasPrecision(3);
            entity.Property(e => e.otpHash).HasMaxLength(500);
            entity.Property(e => e.phoneNumber).HasMaxLength(30);
            entity.Property(e => e.purpose).HasMaxLength(30);
            entity.Property(e => e.verifiedAt).HasPrecision(3);

            entity.HasOne(d => d.user).WithMany(p => p.OtpChallenges)
                .HasForeignKey(d => d.userId)
                .HasConstraintName("FK_OtpChallenge_User");
        });

        modelBuilder.Entity<PackageInfo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__PackageI__3213E83F3682EC76");

            entity.ToTable("PackageInfo");

            entity.HasIndex(e => e.listingId, "UQ__PackageI__5A0F3D98E99761F1").IsUnique();

            entity.Property(e => e.heightCm).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.lengthCm).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.weightKg).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.widthCm).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.listing).WithOne(p => p.PackageInfo)
                .HasForeignKey<PackageInfo>(d => d.listingId)
                .HasConstraintName("FK_PackageInfo_Listing");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Payment__3213E83FF13192C2");

            entity.ToTable("Payment");

            entity.HasIndex(e => new { e.orderId, e.createdAt }, "IX_Payment_OrderCreatedAt").IsDescending(false, true);

            entity.HasIndex(e => e.status, "IX_Payment_Status");

            entity.HasIndex(e => e.transactionId, "UX_Payment_TransactionId")
                .IsUnique()
                .HasFilter("([transactionId] IS NOT NULL)");

            entity.Property(e => e.amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.failureReason).HasMaxLength(500);
            entity.Property(e => e.method).HasMaxLength(30);
            entity.Property(e => e.paidAt).HasPrecision(3);
            entity.Property(e => e.provider).HasMaxLength(50);
            entity.Property(e => e.refundedAt).HasPrecision(3);
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.transactionId).HasMaxLength(150);

            entity.HasOne(d => d.order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.orderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Order");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Product__3213E83F711BC5E2");

            entity.ToTable("Product");

            entity.HasIndex(e => e.categoryId, "IX_Product_Category");

            entity.HasIndex(e => e.title, "IX_Product_Title");

            entity.Property(e => e.brand).HasMaxLength(150);
            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.itemCondition).HasMaxLength(50);
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
            entity.HasKey(e => e.id).HasName("PK__ProductI__3213E83FD996BBB6");

            entity.ToTable("ProductImage");

            entity.Property(e => e.imageUrl).HasMaxLength(1000);

            entity.HasOne(d => d.product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.productId)
                .HasConstraintName("FK_ProductImage_Product");
        });

        modelBuilder.Entity<ReturnRequest>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__ReturnRe__3213E83F3D305B19");

            entity.ToTable("ReturnRequest");

            entity.Property(e => e.approvedAt).HasPrecision(3);
            entity.Property(e => e.completedAt).HasPrecision(3);
            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.reasonCode).HasMaxLength(50);
            entity.Property(e => e.status)
                .HasMaxLength(30)
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
            entity.HasKey(e => e.id).HasName("PK__ReturnRe__3213E83FA5BE1B8B");

            entity.ToTable("ReturnRequestItem");

            entity.HasIndex(e => new { e.returnRequestId, e.orderItemId }, "UQ_ReturnRequestItem").IsUnique();

            entity.HasOne(d => d.orderItem).WithMany(p => p.ReturnRequestItems)
                .HasForeignKey(d => d.orderItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReturnRequestItem_OrderItem");

            entity.HasOne(d => d.returnRequest).WithMany(p => p.ReturnRequestItems)
                .HasForeignKey(d => d.returnRequestId)
                .HasConstraintName("FK_ReturnRequestItem_Request");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Review__3213E83F385E4990");

            entity.ToTable("Review");

            entity.HasIndex(e => new { e.productId, e.createdAt }, "IX_Review_Product").IsDescending(false, true);

            entity.HasIndex(e => new { e.orderItemId, e.reviewerId }, "UQ_Review_OrderItemReviewer").IsUnique();

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.orderItem).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.orderItemId)
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
            entity.HasKey(e => e.id).HasName("PK__SellerFe__3213E83F85446824");

            entity.ToTable("SellerFeedback");

            entity.HasIndex(e => new { e.orderId, e.buyerId }, "UQ_SellerFeedback_OrderBuyer").IsUnique();

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.feedbackType).HasMaxLength(20);

            entity.HasOne(d => d.buyer).WithMany(p => p.SellerFeedbackbuyers)
                .HasForeignKey(d => d.buyerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SellerFeedback_Buyer");

            entity.HasOne(d => d.order).WithMany(p => p.SellerFeedbacks)
                .HasForeignKey(d => d.orderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SellerFeedback_Order");

            entity.HasOne(d => d.seller).WithMany(p => p.SellerFeedbacksellers)
                .HasForeignKey(d => d.sellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SellerFeedback_Seller");
        });

        modelBuilder.Entity<SellerFeedbackSummary>(entity =>
        {
            entity.HasKey(e => e.sellerId).HasName("PK__SellerFe__B5FAE33E1B6B4110");

            entity.ToTable("SellerFeedbackSummary");

            entity.Property(e => e.sellerId).ValueGeneratedNever();
            entity.Property(e => e.averageRating).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.positiveRate).HasColumnType("decimal(7, 4)");
            entity.Property(e => e.updatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.seller).WithOne(p => p.SellerFeedbackSummary)
                .HasForeignKey<SellerFeedbackSummary>(d => d.sellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SellerFeedbackSummary_Seller");
        });

        modelBuilder.Entity<ShippingInfo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Shipping__3213E83F9F8653F2");

            entity.ToTable("ShippingInfo");

            entity.HasIndex(e => e.trackingNumber, "IX_ShippingInfo_Tracking").HasFilter("([trackingNumber] IS NOT NULL)");

            entity.HasIndex(e => e.orderId, "UQ__Shipping__0809335C2E1E513E").IsUnique();

            entity.Property(e => e.carrier).HasMaxLength(100);
            entity.Property(e => e.deliveredAt).HasPrecision(3);
            entity.Property(e => e.estimatedArrival).HasPrecision(3);
            entity.Property(e => e.shippedAt).HasPrecision(3);
            entity.Property(e => e.status)
                .HasMaxLength(30)
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
            entity.HasKey(e => e.id).HasName("PK__Shipping__3213E83F2274753E");

            entity.ToTable("ShippingOption");

            entity.Property(e => e.flatRate).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.isActive).HasDefaultValue(true);
            entity.Property(e => e.serviceName).HasMaxLength(150);
            entity.Property(e => e.serviceType).HasMaxLength(20);

            entity.HasOne(d => d.shippingPolicy).WithMany(p => p.ShippingOptions)
                .HasForeignKey(d => d.shippingPolicyId)
                .HasConstraintName("FK_ShippingOption_Policy");
        });

        modelBuilder.Entity<ShippingPolicy>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Shipping__3213E83F7977B543");

            entity.ToTable("ShippingPolicy");

            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.isActive).HasDefaultValue(true);
            entity.Property(e => e.name).HasMaxLength(150);
            entity.Property(e => e.shippingType).HasMaxLength(20);

            entity.HasOne(d => d.seller).WithMany(p => p.ShippingPolicies)
                .HasForeignKey(d => d.sellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShippingPolicy_Seller");
        });

        modelBuilder.Entity<ShippingRegion>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Shipping__3213E83F475866E1");

            entity.ToTable("ShippingRegion");

            entity.HasIndex(e => e.code, "UQ__Shipping__357D4CF970DD8EF7").IsUnique();

            entity.Property(e => e.code).HasMaxLength(50);
            entity.Property(e => e.isActive).HasDefaultValue(true);
            entity.Property(e => e.name).HasMaxLength(150);
            entity.Property(e => e.regionType).HasMaxLength(20);
            entity.Property(e => e.surcharge).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Store__3213E83F7535F396");

            entity.ToTable("Store");

            entity.HasIndex(e => e.sellerId, "UQ__Store__B5FAE33FAED3504B").IsUnique();

            entity.Property(e => e.bannerImageURL).HasMaxLength(1000);
            entity.Property(e => e.createdAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.storeName).HasMaxLength(150);
            entity.Property(e => e.updatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.seller).WithOne(p => p.Store)
                .HasForeignKey<Store>(d => d.sellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Store_Seller");
        });

        modelBuilder.Entity<TaxRegion>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__TaxRegio__3213E83F7556C88F");

            entity.ToTable("TaxRegion");

            entity.HasIndex(e => e.code, "UQ__TaxRegio__357D4CF9E18888A7").IsUnique();

            entity.Property(e => e.code).HasMaxLength(50);
            entity.Property(e => e.isActive).HasDefaultValue(true);
            entity.Property(e => e.name).HasMaxLength(150);
            entity.Property(e => e.taxRate).HasColumnType("decimal(7, 4)");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__User__3213E83FA3A66D6F");

            entity.ToTable("User");

            entity.HasIndex(e => e.phoneNumber, "IX_User_PhoneNumber").HasFilter("([phoneNumber] IS NOT NULL)");

            entity.HasIndex(e => e.username, "IX_User_Username");

            entity.HasIndex(e => e.email, "UQ__User__AB6E61643B832A92").IsUnique();

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
            entity.HasKey(e => e.id).HasName("PK__Verifica__3213E83FF6529BBE");

            entity.ToTable("VerificationToken");

            entity.HasIndex(e => new { e.userId, e.tokenType, e.expiresAt }, "IX_VerificationToken_UserType");

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
            entity.HasKey(e => e.id).HasName("PK__Watchlis__3213E83F23E2BF3E");

            entity.ToTable("Watchlist");

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

            entity.Property(e => e.currentPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.endTime).HasPrecision(3);
            entity.Property(e => e.listingType).HasMaxLength(20);
            entity.Property(e => e.price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.publishedAt).HasPrecision(3);
            entity.Property(e => e.startTime).HasPrecision(3);
            entity.Property(e => e.status).HasMaxLength(20);
            entity.Property(e => e.title).HasMaxLength(255);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
