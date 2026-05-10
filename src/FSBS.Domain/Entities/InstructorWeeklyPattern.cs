namespace FSBS.Domain.Entities;

/// <summary>
/// A recurring weekly schedule of working hours declared by (or for) an
/// <see cref="Instructor"/>. Acts as the baseline that the schedule resolver
/// projects across every date in the requested window. Concrete
/// <see cref="InstructorAvailability"/> rows then layer on top: extra
/// <c>Available</c> windows add hours; <c>Leave</c> / <c>Other</c> windows
/// subtract them.
/// </summary>
/// <remarks>
/// Updates supersede the previous pattern atomically: the open pattern is
/// closed (its <see cref="EffectiveTo"/> is set to "today") and a new pattern
/// is inserted with <see cref="EffectiveFrom"/> = today and <see cref="EffectiveTo"/> null.
/// A partial unique index on <c>(instructor_id) WHERE effective_to IS NULL</c>
/// guarantees there is at most one open pattern per instructor at any time.
/// </remarks>
public class InstructorWeeklyPattern : AuditableEntity, ISoftDeletable
{
    /// <summary>The instructor this pattern applies to.</summary>
    public Guid InstructorId { get; set; }

    /// <summary>The first date this pattern is in force.</summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>
    /// The first date on which this pattern is no longer in force, or <c>null</c>
    /// if the pattern is currently open.
    /// </summary>
    public DateOnly? EffectiveTo { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the instructor that owns this pattern.</summary>
    public Instructor Instructor { get; set; } = null!;

    /// <summary>The 30-minute-aligned working slots that make up this pattern.</summary>
    public ICollection<InstructorWeeklyPatternSlot> Slots { get; set; } = [];
}
