using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.BuyerSchema.Models;

public partial class GuestCheckoutAccess
{
    public long id { get; set; }

    public long checkoutGroupId { get; set; }

    public string tokenHash { get; set; } = null!;

    public DateTime expiresAt { get; set; }

    public DateTime? revokedAt { get; set; }

    public DateTime createdAt { get; set; }

    public virtual CheckoutGroup checkoutGroup { get; set; } = null!;
}
