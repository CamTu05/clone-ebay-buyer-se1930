using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class User
{
    public int id { get; set; }

    public string username { get; set; } = null!;

    public string email { get; set; } = null!;

    public string? passwordHash { get; set; }

    public string role { get; set; } = null!;

    public string? avatarURL { get; set; }

    public string? phoneNumber { get; set; }

    public bool emailVerified { get; set; }

    public bool phoneVerified { get; set; }

    public string accountStatus { get; set; } = null!;

    public DateTime createdAt { get; set; }

    public DateTime updatedAt { get; set; }

    public DateTime? lastLoginAt { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual ICollection<Auction> AuctionhighestBidders { get; set; } = new List<Auction>();

    public virtual ICollection<Auction> Auctionwinners { get; set; } = new List<Auction>();

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<CheckoutGroup> CheckoutGroups { get; set; } = new List<CheckoutGroup>();

    public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();

    public virtual ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();

    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();

    public virtual ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();

    public virtual ICollection<GiftCard> GiftCards { get; set; } = new List<GiftCard>();

    public virtual ICollection<Listing> Listings { get; set; } = new List<Listing>();

    public virtual ICollection<Message> Messagereceivers { get; set; } = new List<Message>();

    public virtual ICollection<Message> Messagesenders { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<OrderTable> OrderTablebuyers { get; set; } = new List<OrderTable>();

    public virtual ICollection<OrderTable> OrderTablesellers { get; set; } = new List<OrderTable>();

    public virtual ICollection<OtpChallenge> OtpChallenges { get; set; } = new List<OtpChallenge>();

    public virtual ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual SellerFeedbackSummary? SellerFeedbackSummary { get; set; }

    public virtual ICollection<SellerFeedback> SellerFeedbackbuyers { get; set; } = new List<SellerFeedback>();

    public virtual ICollection<SellerFeedback> SellerFeedbacksellers { get; set; } = new List<SellerFeedback>();

    public virtual ICollection<ShippingPolicy> ShippingPolicies { get; set; } = new List<ShippingPolicy>();

    public virtual Store? Store { get; set; }

    public virtual ICollection<VerificationToken> VerificationTokens { get; set; } = new List<VerificationToken>();

    public virtual ICollection<Watchlist> Watchlists { get; set; } = new List<Watchlist>();
}
