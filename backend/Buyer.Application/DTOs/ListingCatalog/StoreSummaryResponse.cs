using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.DTOs.ListingCatalog;

public sealed record StoreSummaryResponse(int StoreId, string StoreName, string? BannerImageUrl);
