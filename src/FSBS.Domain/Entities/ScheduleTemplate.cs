namespace FSBS.Domain.Entities;

/// <summary>
/// Defines the standard operating hours for a specific <see cref="SimulatorBay"/>
/// and <see cref="SimulatorConfiguration"/> on a given day of the week.
/// ScheduleAdmin applies templates to generate recurring calendar blocks without
/// building each booking from scratch.
/// </summary>
public class ScheduleTemplate : AuditableEntity, ISoftDeletable
{
    /// <summary>The bay this template applies to.</summary>
    public Guid BayId { get; set; }

    /// <summary>The simulator configuration in use during this template's hours.</summary>
    public Guid ConfigId { get; set; }

    /// <summary>
    /// ISO day of week (1 = Monday … 7 = Sunday) this template covers.
    /// </summary>
    public int DayOfWeek { get; set; }

    /// <summary>Local time of day at which the bay opens under this template.</summary>
    public TimeOnly OpenTime { get; set; }

    /// <summary>Local time of day at which the bay closes under this template.</summary>
    public TimeOnly CloseTime { get; set; }

    /// <summary>The first calendar date on which this template is effective.</summary>
    public DateOnly ValidFrom { get; set; }

    /// <summary>
    /// The last calendar date on which this template is effective.
    /// <c>null</c> means the template has no planned end date.
    /// </summary>
    public DateOnly? ValidTo { get; set; }

    /// <summary>Whether this template is currently active and applied to the calendar.</summary>
    public bool IsActive { get; set; } = true;

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the bay this template applies to.</summary>
    public SimulatorBay Bay { get; set; } = null!;

    /// <summary>Navigation to the simulator configuration in use.</summary>
    public SimulatorConfiguration Configuration { get; set; } = null!;
}
