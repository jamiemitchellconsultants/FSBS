using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Domain.Interfaces;
using FSBS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

internal sealed class PricingPolicyRepository(FsbsDbContext db) : IPricingPolicyRepository
{
    public Task<PricingPolicy?> FindApplicableAsync(
        Guid configurationId,
        TrainingType trainingType,
        string customerClass,
        DateTimeOffset effectiveDate,
        CancellationToken ct = default)
    {
        var onDate = DateOnly.FromDateTime(effectiveDate.UtcDateTime);
        return db.PricingPolicies
            .Where(p => p.ConfigurationId == configurationId
                     && p.TrainingType    == trainingType
                     && p.CustomerClass   == customerClass
                     && p.EffectiveFrom   <= onDate
                     && (p.EffectiveTo == null || p.EffectiveTo >= onDate))
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<DiscountRule>> GetDiscountRulesAsync(
        Guid policyId,
        CancellationToken ct = default) =>
        await db.DiscountRules
            .Where(r => r.PricingPolicyId == policyId)
            .OrderByDescending(r => r.Priority)
            .ToListAsync(ct);
}
