using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.DTOs.ListingCatalog;

public sealed record CategorySummaryResponse(int CategoryId, string Name, string Slug);
