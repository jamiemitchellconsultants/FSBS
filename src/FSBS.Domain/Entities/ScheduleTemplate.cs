namespace FSBS.Domain.Entities;

/// <summary>
/// A reusable scheduling blueprint associated with a specific
/// <see cref="SimulatorConfiguration"/>. Templates define the standard
/// session patterns (e.g. recurring weekly blocks) that ScheduleAdmin can
/// apply to the calendar without building each booking from scratch.
/// </summary>
public class ScheduleTemplate : AuditableEntity, ISoftDeletable
{
    /// <summary>
    /// The simulator configuration this template is designed for. Only bays
    /// of the owning <see cref="SimulatorUnit"/> running this configuration
    /// are eligible when the template is applied.
    /// </summary>
    public Guid ConfigurationId { get; set; }

    /// <summary>Human-readable name displayed in the template picker (e.g. "Standard B737 Week").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description of intended usage or session pattern.</summary>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the simulator configuration this template targets.</summary>
    public SimulatorConfiguration Configuration { get; set; } = null!;
}
