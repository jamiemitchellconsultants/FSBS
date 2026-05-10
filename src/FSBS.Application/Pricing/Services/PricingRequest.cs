using FSBS.Domain.Enums;

namespace FSBS.Application.Pricing.Services;

/// <summary>
/// Input to the pricing engine. Supplied by the booking command handler at
/// the point of slot confirmation.
/// </summary>
public record PricingRequest(
    Guid ConfigurationId,
    TrainingType TrainingType,
    string CustomerClass,
    AppRole BookerRole,
    int DurationMins,
    int StudentCount,
    DateTimeOffset SlotStart,
    Guid? OrgId,
    int OrgConfirmedSessionCount = 0);
