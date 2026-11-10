using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.DTOs.ListingCatalog;

public sealed record ListingSummaryResponse(
    int ListingId,
    int? ProductId,
    string Title,
    string? PrimaryImageUrl,
    string? Brand,
    string? ItemCondition,
    int SellerId,
    string ListingType,
    decimal EffectivePrice,
    bool FreeShipping,
    decimal? CurrentBid,
    int? BidCount,
    DateTime? AuctionEndTime,
    DateTime? PublishedAt);
