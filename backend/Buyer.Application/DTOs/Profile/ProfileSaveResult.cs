namespace Buyer.Application.DTOs.Profile;

public sealed record ProfileSaveResult(
    ProfileSaveStatus Status,
    ProfileUserSnapshot? UpdatedProfile = null);
