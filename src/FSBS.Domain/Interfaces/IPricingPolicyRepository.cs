using FSBS.Domain.Entities;
using FSBS.Domain.Enums;

namespace FSBS.Domain.Interfaces;

/// <summary>
/// Read-side repository for pricing policies and discount rules.
/// Used exclusively by <c>PricingService</c> at booking-confirmation time.
/// </summary>
public interface IPricingPolicyRepository
{
    /// <summary>
    /// Returns the pricing policy effective on the given date for the
    /// combination of configuration, training type, and customer class.
    /// Returns null if no policy is defined (configuration error).
    /// </summary>
    Task<PricingPolicy?> FindApplicableAsync(
        Guid configurationId,
        TrainingType trainingType,
        string customerClass,
        DateTimeOffset effectiveDate,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all active discount rules attached to the given policy,
    /// ordered by priority descending (highest priority first).
    /// </summary>
    Task<IReadOnlyList<DiscountRule>> GetDiscountRulesAsync(
        Guid policyId,
        CancellationToken ct = default);
}
