using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.DTOs.ListingCatalog;

public sealed class ListingListRequest : IValidatableObject
{
    [StringLength(200)]
    public string? Search { get; init; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; init; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? MinPrice { get; init; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? MaxPrice { get; init; }

    [StringLength(50)]
    public string? Condition { get; init; }

    [StringLength(150)]
    public string? Brand { get; init; }

    public string? ListingType { get; init; }

    [Range(1, int.MaxValue)]
    public int? SellerId { get; init; }

    public bool? FreeShipping { get; init; }
    public bool? AllowBestOffer { get; init; }

    [Range(0, int.MaxValue)]
    public int? MinBidCount { get; init; }

    [Range(0, int.MaxValue)]
    public int? MaxBidCount { get; init; }

    public string? Sort { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
            yield return new ValidationResult(
                "MinPrice must be less than or equal to MaxPrice.",
                [nameof(MinPrice), nameof(MaxPrice)]);

        if (MinBidCount.HasValue && MaxBidCount.HasValue && MinBidCount > MaxBidCount)
            yield return new ValidationResult(
                "MinBidCount must be less than or equal to MaxBidCount.",
                [nameof(MinBidCount), nameof(MaxBidCount)]);

        var listingType = ListingType?.Trim().ToUpperInvariant();
        if (listingType is not null and not "" and not "FIXED_PRICE" and not "AUCTION")
            yield return new ValidationResult(
                "ListingType must be FIXED_PRICE or AUCTION.", [nameof(ListingType)]);

        var sort = Sort?.Trim().ToLowerInvariant();
        if (sort is not null and not "" and not "relevance" and not "newest" and
            not "price_asc" and not "price_desc" and not "ending_soon")
            yield return new ValidationResult(
                "Sort must be relevance, newest, price_asc, price_desc, or ending_soon.", [nameof(Sort)]);

        if (sort == "ending_soon" && listingType == "FIXED_PRICE")
            yield return new ValidationResult(
                "ending_soon cannot be combined with FIXED_PRICE.", [nameof(Sort), nameof(ListingType)]);
    }
}
