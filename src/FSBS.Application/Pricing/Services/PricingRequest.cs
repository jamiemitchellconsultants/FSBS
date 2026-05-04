using FSBS.Domain.Enums;

namespace FSBS.Application.Pricing.Services;

/// <summary>
/// Input to the pricing engine. Supplied by the booking command handler at
/// the point of slot confirmation.
/// </summary>
public record PricingRequest(
    Guid ConfigurationId,
    TrainingType TrainingType,
    CustomerClass CustomerClass,
    AppRole BookerRole,
    int DurationMins,
    int StudentCount,
    DateTimeOffset SlotStart,
    Guid? OrgId,
    /// <summary>
    /// Number of confirmed sessions already booked by this organisation.
    /// Used to evaluate VolumeOrgSession discount eligibility.
    /// Pass 0 for non-corporate bookings.
    /// </summary>
    int OrgConfirmedSessionCount = 0);
