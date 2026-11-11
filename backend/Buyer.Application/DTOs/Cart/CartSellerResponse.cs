namespace Buyer.Application.DTOs.Cart;

public sealed record CartSellerResponse(int Id, string Username, string? StoreName = null);
