using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.DTOs.ListingCatalog;

public sealed record ProductImageResponse(int ImageId, string ImageUrl, bool IsPrimary, int SortOrder);
