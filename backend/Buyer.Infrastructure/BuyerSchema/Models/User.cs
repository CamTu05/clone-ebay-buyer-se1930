using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.BuyerSchema.Models;

public partial class User
{
    public int authVersion { get; set; }
    public DateTime? usernameChangedAt { get; set; }
    public int id { get; set; }

    public string username { get; set; } = null!;

    public string email { get; set; } = null!;

    public string? passwordHash { get; set; }

    public string role { get; set; } = null!;

    public string? avatarURL { get; set; }

    public string? phoneNumber { get; set; }

    public bool emailVerified { get; set; }

    public string accountStatus { get; set; } = null!;

    public DateTime createdAt { get; set; }

    public DateTime updatedAt { get; set; }

    public DateTime? lastLoginAt { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual ICollection<Auction> AuctionhighestBidders { get; set; } = new List<Auction>();

    public virtual ICollection<Auction> Auctionwinners { get; set; } = new List<Auction>();

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public virtual ICollection<BuyerFeedback> BuyerFeedbackbuyers { get; set; } = new List<BuyerFeedback>();

    public virtual ICollection<BuyerFeedback> BuyerFeedbacksellers { get; set; } = new List<BuyerFeedback>();

    public virtual ICollection<CancellationRequest> CancellationRequests { get; set; } = new List<CancellationRequest>();

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<CheckoutGroup> CheckoutGroups { get; set; } = new List<CheckoutGroup>();

    public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();

    public virtual ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();

    public virtual ICollection<Listing> Listings { get; set; } = new List<Listing>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<OrderTable> OrderTablebuyers { get; set; } = new List<OrderTable>();

    public virtual ICollection<OrderTable> OrderTablesellers { get; set; } = new List<OrderTable>();

    public virtual ICollection<ReturnPolicy> ReturnPolicies { get; set; } = new List<ReturnPolicy>();

    public virtual ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<SellerFeedback> SellerFeedbackbuyers { get; set; } = new List<SellerFeedback>();

    public virtual ICollection<SellerFeedback> SellerFeedbacksellers { get; set; } = new List<SellerFeedback>();

    public virtual ICollection<ShippingPolicy> ShippingPolicies { get; set; } = new List<ShippingPolicy>();

    public virtual ICollection<VerificationToken> VerificationTokens { get; set; } = new List<VerificationToken>();

    public virtual ICollection<Watchlist> Watchlists { get; set; } = new List<Watchlist>();
}
