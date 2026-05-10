using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// Defines the base hourly rate for a specific combination of simulator
/// configuration, training type, and customer class, effective for a given
/// date range. This is the starting point for all price calculations.
/// </summary>
/// <remarks>
/// Price is calculated at booking confirmation time by selecting the most
/// recently effective policy for the booking's
/// (<see cref="ConfigurationId"/>, <see cref="TrainingType"/>,
/// <see cref="CustomerClass"/>) combination, then applying any applicable
/// <see cref="DiscountRule"/>s. Once a <see cref="Booking"/> is confirmed
/// the price is locked — no recalculation occurs even if the policy changes.
/// Staff (InternalStudent) bookings always use the staff rate and are
/// ineligible for any discount rules.
/// </remarks>
public class PricingPolicy : AuditableEntity, ISoftDeletable
{
    /// <summary>
    /// The simulator configuration this rate applies to. Different aircraft
    /// types and cabin layouts carry different cost structures.
    /// </summary>
    public Guid ConfigurationId { get; set; }

    /// <summary>
    /// Whether this rate is for Flight Deck or Cabin Crew training. The two
    /// training types have distinct pricing even on the same configuration.
    /// </summary>
    public TrainingType TrainingType { get; set; }

    /// <summary>
    /// Customer segment this rate applies to. Standard, Staff, and Corporate
    /// customers each have their own rate ladder.
    /// </summary>
    public string CustomerClass { get; set; } = string.Empty;

    /// <summary>Base rate in GBP per hour before any discounts are applied.</summary>
    public decimal HourlyRateGbp { get; set; }

    /// <summary>
    /// The date from which this policy is active (inclusive). When multiple
    /// policies match the same key tuple the one with the latest
    /// <c>EffectiveFrom</c> is selected.
    /// </summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>
    /// The last date on which this policy is active (inclusive). <c>null</c>
    /// means the policy has no planned expiry.
    /// </summary>
    public DateOnly? EffectiveTo { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the simulator configuration this policy covers.</summary>
    public SimulatorConfiguration Configuration { get; set; } = null!;

    /// <summary>Discount rules that can reduce the base rate for eligible bookings.</summary>
    public ICollection<DiscountRule> DiscountRules { get; set; } = [];
}
