using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.BuyerSchema.Models;

public partial class OrderTable
{
    public long id { get; set; }

    public long checkoutGroupId { get; set; }

    public int? buyerId { get; set; }

    public int sellerId { get; set; }

    public int? addressId { get; set; }

    public int? shippingOptionId { get; set; }

    public string shippingFullName { get; set; } = null!;

    public string shippingEmail { get; set; } = null!;

    public string shippingPhone { get; set; } = null!;

    public string shippingStreet { get; set; } = null!;

    public string? shippingAddressLine2 { get; set; }

    public string shippingCity { get; set; } = null!;

    public string? shippingState { get; set; }

    public string? shippingPostalCode { get; set; }

    public string shippingCountry { get; set; } = null!;

    public string? shippingCountryCode { get; set; }

    public string? shippingServiceName { get; set; }

    public decimal subtotal { get; set; }

    public decimal discountAmount { get; set; }

    public decimal shippingFee { get; set; }

    public decimal totalAmount { get; set; }

    public string currency { get; set; } = null!;

    public string status { get; set; } = null!;

    public DateTime? stockRestoredAt { get; set; }

    public DateTime orderDate { get; set; }

    public DateTime updatedAt { get; set; }

    public virtual ICollection<CancellationRequest> CancellationRequests { get; set; } = new List<CancellationRequest>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();

    public virtual SellerFeedback? SellerFeedback { get; set; }

    public virtual ShippingInfo? ShippingInfo { get; set; }

    public virtual Address? address { get; set; }

    public virtual User? buyer { get; set; }

    public virtual CheckoutGroup checkoutGroup { get; set; } = null!;

    public virtual User seller { get; set; } = null!;

    public virtual ShippingOption? shippingOption { get; set; }
}
