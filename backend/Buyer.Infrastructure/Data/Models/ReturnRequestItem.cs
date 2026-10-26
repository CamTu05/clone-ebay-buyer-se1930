using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class ReturnRequestItem
{
    public long id { get; set; }

    public long returnRequestId { get; set; }

    public long orderItemId { get; set; }

    public int quantity { get; set; }

    public virtual OrderItem orderItem { get; set; } = null!;

    public virtual ReturnRequest returnRequest { get; set; } = null!;
}
