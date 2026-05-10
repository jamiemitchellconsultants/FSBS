using FSBS.Application.Common.Interfaces;
using FSBS.Shared.UserProfiles;
using MediatR;

namespace FSBS.Application.UserProfiles.Queries;

public sealed class GetMyProfileHandler(
    IUserProfileRepository profiles,
    ICurrentUser currentUser,
    IS3Service s3)
    : IRequestHandler<GetMyProfileQuery, UserProfileDto?>
{
    public async Task<UserProfileDto?> Handle(GetMyProfileQuery request, CancellationToken ct)
    {
        var profile = await profiles.GetByUserIdAsync(currentUser.UserId, ct);
        if (profile is null)
            return null;

        string? photoUrl = null;
        if (!string.IsNullOrWhiteSpace(profile.PhotoS3Key))
            photoUrl = await s3.GeneratePresignedGetUrlAsync(profile.PhotoS3Key, expiryMinutes: 60, ct);

        return new UserProfileDto(
            profile.FirstName,
            profile.LastName,
            profile.PhoneNumber,
            profile.DateOfBirth,
            profile.LicenceNumber,
            profile.LicenceExpiry,
            profile.PhotoS3Key,
            photoUrl);
    }
}
