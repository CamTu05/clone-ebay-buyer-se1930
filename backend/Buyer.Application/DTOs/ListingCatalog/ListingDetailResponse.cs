using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.DTOs.ListingCatalog;

public sealed record ListingDetailResponse(
    int ListingId,
    int? ProductId,
    string Title,
    string? Description,
    string? Brand,
    string? ItemCondition,
    CategorySummaryResponse Category,
    IReadOnlyList<ProductImageResponse> Images,
    int SellerId,
    string SellerUsername,
    string ListingType,
    decimal EffectivePrice,
    decimal? BuyItNowPrice,
    int Quantity,
    ShippingSummaryResponse Shipping,
    AuctionDetailResponse? Auction,
    DateTime? PublishedAt);
