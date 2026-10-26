using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Message
{
    public long id { get; set; }

    public int senderId { get; set; }

    public int receiverId { get; set; }

    public string content { get; set; } = null!;

    public DateTime sentAt { get; set; }

    public bool isRead { get; set; }

    public virtual User receiver { get; set; } = null!;

    public virtual User sender { get; set; } = null!;
}
