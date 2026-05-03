using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// A threshold-based discount rule attached to a <see cref="PricingPolicy"/>.
/// Rules are evaluated at booking confirmation and the resulting deduction is
/// snapshotted as an immutable <see cref="BookingDiscount"/>.
/// </summary>
/// <remarks>
/// <b>Evaluation algorithm:</b>
/// <list type="number">
///   <item>Collect all rules whose thresholds are satisfied by the booking.</item>
///   <item>Sort by <see cref="Priority"/> descending.</item>
///   <item>Apply the highest-priority rule. If it is <see cref="IsCombinable"/>,
///     also sum all other combinable rules below it.</item>
///   <item>Cap the total discount at the maximum discount ceiling configured on
///     the policy.</item>
/// </list>
/// Staff (InternalStudent) bookings are exempt — no discount rules apply.
/// </remarks>
public class DiscountRule : AuditableEntity, ISoftDeletable
{
    /// <summary>The pricing policy this rule belongs to.</summary>
    public Guid PricingPolicyId { get; set; }

    /// <summary>
    /// Category of discount (e.g. volume-based, advance-booking, corporate
    /// negotiated). Determines which threshold fields in <see cref="ThresholdJson"/>
    /// are evaluated.
    /// </summary>
    public DiscountType DiscountType { get; set; }

    /// <summary>
    /// Evaluation order when multiple rules match. Higher number wins.
    /// Among rules at equal priority, combinable rules are summed.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>Percentage reduction applied to the base price (0–100).</summary>
    public decimal DiscountPct { get; set; }

    /// <summary>
    /// When <c>true</c> this rule can be stacked with other combinable rules.
    /// When <c>false</c> it is applied exclusively — no other rule is combined
    /// with it even if other combinable rules are present.
    /// </summary>
    public bool IsCombinable { get; set; }

    /// <summary>
    /// JSON document containing the rule's eligibility criteria
    /// (e.g. <c>{"minSessions": 5}</c> for a volume rule, or
    /// <c>{"minDaysAhead": 30}</c> for an advance-booking rule).
    /// Stored as <c>jsonb</c>; interpreted by the pricing service at confirm time.
    /// </summary>
    public string? ThresholdJson { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the pricing policy this rule belongs to.</summary>
    public PricingPolicy PricingPolicy { get; set; } = null!;
}
