namespace FSBS.Domain.Entities;

/// <summary>
/// A single working interval within a recurring <see cref="InstructorWeeklyPattern"/>.
/// Times are 30-minute-aligned and naive — the <c>InstructorScheduleResolver</c>
/// projects them onto a date using the school's local time zone (Europe/London)
/// before producing UTC effective intervals.
/// </summary>
public class InstructorWeeklyPatternSlot : AuditableEntity
{
    /// <summary>The pattern this slot belongs to. Cascade deletes with the pattern.</summary>
    public Guid PatternId { get; set; }

    /// <summary>Which day of the week this slot covers.</summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>Start of the working interval (30-minute aligned).</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>End of the working interval (30-minute aligned, strictly after <see cref="StartTime"/>).</summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>Navigation to the pattern that owns this slot.</summary>
    public InstructorWeeklyPattern Pattern { get; set; } = null!;
}
