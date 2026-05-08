namespace FSBS.Domain.Entities;

/// <summary>
/// An instructor or CourseDirector's sign-off confirming that a student has
/// satisfactorily completed a specific <see cref="Lesson"/> within their
/// <see cref="Enrolment"/>. Once created, progress records are treated as
/// immutable audit evidence.
/// </summary>
public class ProgressRecord : AuditableEntity, ISoftDeletable
{
    /// <summary>The enrolment (student + course) this record belongs to.</summary>
    public Guid EnrolmentId { get; set; }

    /// <summary>The lesson that was completed.</summary>
    public Guid LessonId { get; set; }

    /// <summary>UTC timestamp at which the lesson was completed in the simulator.</summary>
    public DateTimeOffset CompletedAt { get; set; }

    /// <summary>
    /// The instructor who delivered and signed off this lesson.
    /// <c>null</c> if signed off by a CourseDirector without an assigned instructor.
    /// </summary>
    public Guid? InstructorId { get; set; }

    /// <summary>
    /// Optional grade awarded for this lesson (e.g. "1", "2", "3", "4" on the
    /// standard EASA grading scale, or a school-defined code).
    /// </summary>
    public string? Grade { get; set; }

    /// <summary>
    /// Optional debrief notes or performance observations recorded by the
    /// signing instructor.
    /// </summary>
    public string? Notes { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the parent enrolment.</summary>
    public Enrolment Enrolment { get; set; } = null!;

    /// <summary>Navigation to the lesson that was completed.</summary>
    public Lesson Lesson { get; set; } = null!;
}
