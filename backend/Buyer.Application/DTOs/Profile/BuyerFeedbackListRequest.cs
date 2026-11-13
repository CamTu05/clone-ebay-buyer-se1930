using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.DTOs.Profile;

public sealed class BuyerFeedbackListRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;
}
