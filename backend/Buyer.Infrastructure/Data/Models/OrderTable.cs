using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class OrderTable
{
    public long id { get; set; }

    public long? checkoutGroupId { get; set; }

    public int? buyerId { get; set; }

    public string? guestEmail { get; set; }

    public int sellerId { get; set; }

    public int? addressId { get; set; }

    public DateTime orderDate { get; set; }

    public DateTime? paymentDeadline { get; set; }

    public string shippingFullName { get; set; } = null!;

    public string shippingPhone { get; set; } = null!;

    public string shippingStreet { get; set; } = null!;

    public string? shippingAddressLine2 { get; set; }

    public string shippingCity { get; set; } = null!;

    public string? shippingState { get; set; }

    public string? shippingPostalCode { get; set; }

    public string shippingCountry { get; set; } = null!;

    public string? shippingCountryCode { get; set; }

    public int? shippingOptionId { get; set; }

    public string? shippingServiceName { get; set; }

    public string? shippingServiceType { get; set; }

    public decimal subtotal { get; set; }

    public decimal discountAmount { get; set; }

    public decimal shippingFee { get; set; }

    public decimal? taxRateSnapshot { get; set; }

    public decimal taxAmount { get; set; }

    public decimal importCharge { get; set; }

    public decimal totalAmount { get; set; }

    public string currency { get; set; } = null!;

    public int? couponId { get; set; }

    public string status { get; set; } = null!;

    public DateTime createdAt { get; set; }

    public DateTime updatedAt { get; set; }

    public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();

    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();

    public virtual ICollection<GiftCardTransaction> GiftCardTransactions { get; set; } = new List<GiftCardTransaction>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();

    public virtual ICollection<SellerFeedback> SellerFeedbacks { get; set; } = new List<SellerFeedback>();

    public virtual ShippingInfo? ShippingInfo { get; set; }

    public virtual Address? address { get; set; }

    public virtual User? buyer { get; set; }

    public virtual CheckoutGroup? checkoutGroup { get; set; }

    public virtual Coupon? coupon { get; set; }

    public virtual User seller { get; set; } = null!;

    public virtual ShippingOption? shippingOption { get; set; }
}
