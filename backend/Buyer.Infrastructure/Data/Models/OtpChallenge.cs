using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class OtpChallenge
{
    public int id { get; set; }

    public int? userId { get; set; }

    public string phoneNumber { get; set; } = null!;

    public string purpose { get; set; } = null!;

    public string otpHash { get; set; } = null!;

    public DateTime expiresAt { get; set; }

    public int attemptCount { get; set; }

    public int resendCount { get; set; }

    public DateTime? verifiedAt { get; set; }

    public DateTime createdAt { get; set; }

    public virtual User? user { get; set; }
}
