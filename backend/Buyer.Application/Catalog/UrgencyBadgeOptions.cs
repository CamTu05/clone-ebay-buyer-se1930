namespace Buyer.Application.Catalog;

public sealed class UrgencyBadgeOptions
{
    public const string SectionName = "UrgencyBadge";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Priority mode when both thresholds are met and ShowBothIfEligible is false.
    /// Values: "CART_FIRST" | "SOLD_FIRST"
    /// </summary>
    public string PriorityMode { get; set; } = "CART_FIRST";

    /// <summary>
    /// If true, display both in-cart and sold count when both qualify.
    /// </summary>
    public bool ShowBothIfEligible { get; set; } = false;

    /// <summary>
    /// Minimum shopping carts containing this product required to display "In X carts".
    /// </summary>
    public int MinCartThreshold { get; set; } = 2;

    /// <summary>
    /// Minimum units sold required to display "X sold".
    /// </summary>
    public int MinSoldThreshold { get; set; } = 1;

    /// <summary>
    /// Order statuses considered valid for calculating sold units.
    /// </summary>
    public string[] ValidOrderStatuses { get; set; } = ["COMPLETED", "CONFIRMED"];
}

