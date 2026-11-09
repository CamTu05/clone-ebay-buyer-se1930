using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.DTOs.ListingCatalog;

public sealed record ShippingSummaryResponse(
    string? ShippingType,
    IReadOnlyList<ShippingOptionResponse> AvailableOptions);
