namespace FSBS.Domain.Entities;

/// <summary>
/// A chapter or unit within a <see cref="Course"/>, grouping related
/// <see cref="Lesson"/>s into a logical progression block.
/// </summary>
public class Module : AuditableEntity, ISoftDeletable
{
    /// <summary>The course this module belongs to.</summary>
    public Guid CourseId { get; set; }

    /// <summary>
    /// Display title of the module (e.g. "Phase 1 — Normal Procedures").
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 1-based position within the course. Modules are presented to students
    /// in ascending <c>SequenceOrder</c>; earlier modules must typically be
    /// completed before later ones are unlocked.
    /// </summary>
    public int SequenceOrder { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the parent course.</summary>
    public Course Course { get; set; } = null!;

    /// <summary>The lessons contained within this module, ordered by <see cref="Lesson.SequenceOrder"/>.</summary>
    public ICollection<Lesson> Lessons { get; set; } = [];
}
