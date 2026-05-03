using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// Links a student <see cref="AppUser"/> to a <see cref="Course"/> and tracks
/// their overall progress through it. A unique constraint on
/// <c>(user_id, course_id)</c> ensures a student can only be enrolled once
/// per course at a time.
/// </summary>
/// <remarks>
/// <b>Status lifecycle:</b> <c>Active → Completed | Withdrawn | Suspended</c>.
/// <c>Completed</c> is set by a CourseDirector once all lessons have a
/// <see cref="ProgressRecord"/>. <c>Withdrawn</c> or <c>Suspended</c>
/// can be applied administratively without affecting the lesson history.
/// </remarks>
public class Enrolment : AuditableEntity, ISoftDeletable
{
    /// <summary>The student enrolled on the course.</summary>
    public Guid UserId { get; set; }

    /// <summary>The course the student is enrolled on.</summary>
    public Guid CourseId { get; set; }

    /// <summary>Current progress state of the enrolment.</summary>
    public EnrolmentStatus Status { get; set; }

    /// <summary>
    /// The calendar date on which the enrolment was marked <c>Completed</c>.
    /// <c>null</c> while the enrolment is still active.
    /// </summary>
    public DateOnly? CompletedOn { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the enrolled student.</summary>
    public AppUser User { get; set; } = null!;

    /// <summary>Navigation to the enrolled course.</summary>
    public Course Course { get; set; } = null!;

    /// <summary>Sign-off records for each lesson the student has completed.</summary>
    public ICollection<ProgressRecord> ProgressRecords { get; set; } = [];
}
