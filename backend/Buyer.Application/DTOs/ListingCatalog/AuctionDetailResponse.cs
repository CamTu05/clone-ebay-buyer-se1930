using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.DTOs.ListingCatalog;

public sealed record AuctionDetailResponse(
    int AuctionId,
    decimal StartingPrice,
    decimal CurrentPrice,
    decimal BidIncrement,
    int BidCount,
    DateTime StartTime,
    DateTime EndTime,
    string Status);
