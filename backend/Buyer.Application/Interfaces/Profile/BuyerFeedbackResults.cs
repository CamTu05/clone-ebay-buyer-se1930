namespace Buyer.Application.Interfaces.Profile;

public sealed record BuyerFeedbackRepositoryPage(
    int TotalItems,
    IReadOnlyList<BuyerFeedbackItemResponse> Items);

public sealed record BuyerFeedbackServiceResult(
    BuyerFeedbackPageResponse? Value,
    ProfileFailure? Failure,
    IReadOnlyList<ProfileFieldError> Errors)
{
    public bool IsSuccess => Failure is null;

    public static BuyerFeedbackServiceResult Success(BuyerFeedbackPageResponse value) =>
        new(value, null, []);

    public static BuyerFeedbackServiceResult Failed(ProfileFailure failure, params ProfileFieldError[] errors) =>
        new(null, failure, errors);
}
