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
    /// Display title of the lesson (e.g. "Emergency Descent Procedure").
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 1-based position within the parent module. Lessons are presented in
    /// ascending <c>SequenceOrder</c>.
    /// </summary>
    public int SequenceOrder { get; set; }

    /// <summary>
    /// Minimum duration of this lesson in minutes. Used when building a
    /// booking slot for the session; the actual <see cref="BookingSlot.DurationMins"/>
    /// must be at least 240 minutes regardless of the lesson's own duration.
    /// </summary>
    public int MinDurationMins { get; set; }

    /// <summary>Whether this lesson requires an instructor to be assigned.</summary>
    public bool RequiresInstructor { get; set; } = true;

    /// <summary>Whether this lesson is mandatory for course completion.</summary>
    public bool IsMandatory { get; set; } = true;

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the parent module.</summary>
    public Module Module { get; set; } = null!;
}
