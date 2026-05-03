namespace FSBS.Domain.Entities;

/// <summary>
/// A scheduled maintenance or downtime period for a <see cref="SimulatorUnit"/>.
/// During this window the unit's bays are blocked and the calendar renders
/// them as dark grey (non-selectable).
/// </summary>
public class MaintenanceWindow : AuditableEntity, ISoftDeletable
{
    /// <summary>The simulator unit that is unavailable during this window.</summary>
    public Guid SimulatorUnitId { get; set; }

    /// <summary>UTC start of the maintenance period.</summary>
    public DateTimeOffset StartAt { get; set; }

    /// <summary>UTC end of the maintenance period.</summary>
    public DateTimeOffset EndAt { get; set; }

    /// <summary>
    /// Optional description of the work being performed (e.g. "Scheduled
    /// annual software update", "Motion platform bearing replacement").
    /// </summary>
    public string? Reason { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the simulator unit being maintained.</summary>
    public SimulatorUnit SimulatorUnit { get; set; } = null!;
}
