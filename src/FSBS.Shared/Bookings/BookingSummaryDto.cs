namespace FSBS.Shared.Bookings;

public record BookingSummaryDto(
    Guid Id,
    string Status,
    string TrainingType,
    string AircraftType,
    string SimulatorUnitName,
    string BayName,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    int DurationMins,
    int StudentCount,
    string? InstructorName,
    decimal? NetPriceGbp,
    string BookerRole,
    Guid? BookerUserId = null,
    Guid? BookerOrgId = null
);
