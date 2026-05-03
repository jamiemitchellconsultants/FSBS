namespace FSBS.Domain.Entities;

/// <summary>
/// The physical flight simulator device (e.g. a Full-Flight Simulator or
/// Flight Training Device). A unit contains one or more <see cref="SimulatorBay"/>s
/// and can operate under different <see cref="SimulatorConfiguration"/>s.
/// </summary>
/// <remarks>
/// Reconfiguring a unit from one configuration to another requires a turnaround
/// window. The duration is sourced first from a matching
/// <see cref="ReconfigurationTemplate"/>; when no template exists the fallback
/// is <see cref="DefaultReconfigMins"/>.
/// </remarks>
public class SimulatorUnit : AuditableEntity, ISoftDeletable
{
    /// <summary>Human-readable name displayed in the scheduling calendar (e.g. "FFS-1").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional physical location description (e.g. "Bay A, Building 3").</summary>
    public string? Location { get; set; }

    /// <summary>
    /// The <see cref="SimulatorConfiguration"/> the unit is currently set up for.
    /// Determines which training types and pricing tiers apply to new bookings.
    /// Changes when a <see cref="ReconfigurationSlot"/> completes.
    /// </summary>
    public Guid? ActiveConfigurationId { get; set; }

    /// <summary>
    /// Fallback reconfiguration turnaround time in minutes used when no
    /// <see cref="ReconfigurationTemplate"/> exists for a specific config-to-config pair.
    /// </summary>
    public int DefaultReconfigMins { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the currently active configuration.</summary>
    public SimulatorConfiguration? ActiveConfiguration { get; set; }

    /// <summary>All configurations (past and present) that have been defined for this unit.</summary>
    public ICollection<SimulatorConfiguration> Configurations { get; set; } = [];

    /// <summary>The physical bays that make up this simulator unit.</summary>
    public ICollection<SimulatorBay> Bays { get; set; } = [];

    /// <summary>Scheduled maintenance periods during which this unit is unavailable.</summary>
    public ICollection<MaintenanceWindow> MaintenanceWindows { get; set; } = [];
}
