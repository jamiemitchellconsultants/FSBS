namespace FSBS.Domain.Entities;

/// <summary>
/// A single deliverable training session within a <see cref="Module"/>.
/// When a student completes a lesson an instructor or CourseDirector creates
/// a <see cref="ProgressRecord"/> to sign it off.
/// </summary>
public class Lesson : AuditableEntity, ISoftDeletable
{
    /// <summary>The module this lesson belongs to.</summary>
    public Guid ModuleId { get; set; }

    /// <summary>
    /// Display name of the lesson (e.g. "Emergency Descent Procedure").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 1-based position within the parent module. Lessons are presented in
    /// ascending <c>SequenceOrder</c>.
    /// </summary>
    public int SequenceOrder { get; set; }

    /// <summary>
    /// Standard duration of this lesson in minutes. Used when building a
    /// booking slot for the session; the actual <see cref="BookingSlot.DurationMins"/>
    /// must be at least 240 minutes regardless of the lesson's own duration.
    /// </summary>
    public int DurationMins { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the parent module.</summary>
    public Module Module { get; set; } = null!;
}
