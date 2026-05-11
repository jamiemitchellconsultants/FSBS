using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Entities;
using MediatR;

namespace FSBS.Application.UserProfiles.Commands;

public sealed class UpdateMyProfileHandler(IUserProfileRepository profiles, ICurrentUser currentUser)
    : IRequestHandler<UpdateMyProfileCommand, Unit>
{
    public async Task<Unit> Handle(UpdateMyProfileCommand request, CancellationToken ct)
    {
        var p = request.Profile;
        var profile = new UserProfile
        {
            Id            = currentUser.UserId,
            FirstName     = p.FirstName,
            LastName      = p.LastName,
            PhoneNumber   = p.PhoneNumber,
            DateOfBirth   = p.DateOfBirth,
            LicenceNumber = p.LicenceNumber,
            LicenceExpiry = p.LicenceExpiry,
            PhotoS3Key    = p.PhotoS3Key
        };
        await profiles.UpsertAsync(profile, ct);
        return Unit.Value;
    }
}
