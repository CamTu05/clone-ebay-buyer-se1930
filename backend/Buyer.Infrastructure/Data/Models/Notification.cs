using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Notification
{
    public long id { get; set; }

    public int userId { get; set; }

    public string type { get; set; } = null!;

    public string title { get; set; } = null!;

    public string? content { get; set; }

    public string? referenceType { get; set; }

    public string? referenceId { get; set; }

    public bool isRead { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime? readAt { get; set; }

    public virtual User user { get; set; } = null!;
}
