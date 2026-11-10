using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.DTOs.ListingCatalog;

public sealed record ShippingOptionResponse(
    int ShippingOptionId,
    string ServiceName,
    string ServiceType,
    decimal? FlatRate,
    int? EstimatedMinDays,
    int? EstimatedMaxDays);
