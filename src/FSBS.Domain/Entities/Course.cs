using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// A structured training programme comprising an ordered set of
/// <see cref="Module"/>s and <see cref="Lesson"/>s. Students are linked to a
/// course via <see cref="Enrolment"/> and progress through it lesson by lesson.
/// </summary>
/// <remarks>
/// Courses are tenant-scoped — a corporate organisation's courses are invisible
/// to other tenants. The <see cref="TrainingType"/> determines whether the course
/// is for Flight Deck or Cabin Crew training, which in turn constrains which
/// simulator configurations are eligible for its sessions.
/// </remarks>
public class Course : AuditableEntity, ISoftDeletable, ITenantScoped
{
    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    /// <summary>Display title of the course shown to students and instructors.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional overview or objectives for the course.</summary>
    public string? Description { get; set; }

    /// <summary>Regulatory framework this course satisfies (e.g. "EASA Part-FCL", "UK CAA").</summary>
    public string? RegulatoryFramework { get; set; }

    /// <summary>Total scheduled hours for the full course. Must be greater than zero.</summary>
    public decimal TotalHours { get; set; }

    /// <summary>Whether this course is currently active and open for new enrolments.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a Flight Deck or Cabin Crew training programme. Restricts
    /// the simulator configurations that can host sessions for this course.
    /// </summary>
    public TrainingType TrainingType { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>The ordered set of modules that make up this course.</summary>
    public ICollection<Module> Modules { get; set; } = [];

    /// <summary>All student enrolments on this course.</summary>
    public ICollection<Enrolment> Enrolments { get; set; } = [];
}
