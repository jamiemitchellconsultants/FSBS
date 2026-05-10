using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Entities;
using FSBS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

public sealed class UserProfileRepository(FsbsDbContext db) : IUserProfileRepository
{
    public Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == userId, ct);

    public async Task UpsertAsync(UserProfile profile, CancellationToken ct = default)
    {
        var existing = await db.UserProfiles.FirstOrDefaultAsync(p => p.Id == profile.Id, ct);
        if (existing is null)
            db.UserProfiles.Add(profile);
        else
        {
            existing.FirstName     = profile.FirstName;
            existing.LastName      = profile.LastName;
            existing.PhoneNumber   = profile.PhoneNumber;
            existing.DateOfBirth   = profile.DateOfBirth;
            existing.LicenceNumber = profile.LicenceNumber;
            existing.LicenceExpiry = profile.LicenceExpiry;
            if (profile.PhotoS3Key is not null)
                existing.PhotoS3Key = profile.PhotoS3Key;
        }
        await db.SaveChangesAsync(ct);
    }
}
