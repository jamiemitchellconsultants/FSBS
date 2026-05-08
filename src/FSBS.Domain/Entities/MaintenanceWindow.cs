namespace FSBS.Domain.Entities;

/// <summary>
/// A scheduled maintenance or downtime period for a <see cref="SimulatorBay"/>.
/// During this window the bay is blocked and the calendar renders it as dark
/// grey (non-selectable).
/// </summary>
public class MaintenanceWindow : AuditableEntity, ISoftDeletable
{
    /// <summary>The simulator bay that is unavailable during this window.</summary>
    public Guid BayId { get; set; }

    /// <summary>UTC start of the maintenance period.</summary>
    public DateTimeOffset StartAt { get; set; }

    /// <summary>UTC end of the maintenance period.</summary>
    public DateTimeOffset EndAt { get; set; }

    /// <summary>
    /// Description of the work being performed (e.g. "Scheduled annual
    /// software update", "Motion platform bearing replacement").
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the simulator bay being maintained.</summary>
    public SimulatorBay Bay { get; set; } = null!;
}
