using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Domain.Interfaces;
using FSBS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

internal sealed class InstructorRepository(FsbsDbContext db) : IInstructorRepository
{
    public Task<Instructor?> FindByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        db.Instructors
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.UserId == userId, ct);

    public async Task<IReadOnlyList<Instructor>> ListRatedForAsync(
        TrainingType trainingType,
        CancellationToken ct = default) =>
        await db.Instructors
            .Include(i => i.User)
            .Where(i => i.TrainingTypeRatings.Contains(trainingType))
            .OrderBy(i => i.User.Email)
            .ToListAsync(ct);
}
