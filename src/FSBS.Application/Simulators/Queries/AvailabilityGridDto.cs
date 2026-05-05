namespace FSBS.Application.Simulators.Queries;

/// <summary>
/// Represents a single available booking slot on a simulator bay.
/// </summary>
public sealed record AvailableSlotDto(
    Guid BayId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    int RemainingCapacity);

/// <summary>
/// Represents a non-bookable reconfiguration window between two sessions.
/// Rendered as hatched grey on the availability calendar.
/// </summary>
public sealed record ReconfigurationWindowDto(
    Guid BayId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string FromConfiguration,
    string ToConfiguration,
    int DurationMins);

/// <summary>
/// Represents a maintenance window during which the simulator is unavailable.
/// </summary>
public sealed record MaintenanceWindowDto(
    Guid BayId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string? Reason);

/// <summary>
/// Full availability grid returned by <c>GET /simulators/{id}/availability</c>.
/// Cached in Redis for 60 seconds; invalidated on every booking mutation.
/// </summary>
public sealed record AvailabilityGridDto(
    Guid SimulatorId,
    DateTimeOffset From,
    DateTimeOffset To,
    IReadOnlyList<AvailableSlotDto> AvailableSlots,
    IReadOnlyList<ReconfigurationWindowDto> ReconfigurationWindows,
    IReadOnlyList<MaintenanceWindowDto> MaintenanceWindows);
