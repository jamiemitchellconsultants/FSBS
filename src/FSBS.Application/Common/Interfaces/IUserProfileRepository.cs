using FSBS.Domain.Entities;

namespace FSBS.Application.Common.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task UpsertAsync(UserProfile profile, CancellationToken ct = default);
}
