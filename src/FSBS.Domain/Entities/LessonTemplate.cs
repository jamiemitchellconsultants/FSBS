using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// Reusable blueprint for a <see cref="Lesson"/> held in the tenant's
/// curriculum library. Authored once by a CourseDirector or SystemAdmin, then
/// applied to any number of <see cref="Module"/>s. When attached to a Module
/// the template's fields are <em>copied</em> into a new <see cref="Lesson"/>
/// row — subsequent edits to the template do not mutate previously attached
/// lessons (matches the immutable-snapshot convention used elsewhere in FSBS).
/// </summary>
/// <remarks>
/// Templates are tenant-scoped via <see cref="ITenantScoped"/> and soft-deletable
/// via <see cref="ISoftDeletable"/>. Retiring a template (setting
/// <see cref="IsActive"/> to <c>false</c>) hides it from the library picker but
/// preserves it for historical reference; existing attached lessons remain
/// readable and editable in their owning Modules.
/// </remarks>
public class LessonTemplate : AuditableEntity, ISoftDeletable, ITenantScoped
{
    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Display title shown in the library list. Copied into
    /// <see cref="Lesson.Title"/> when the template is attached to a module.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional learning objectives or content summary.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Either <see cref="TrainingType.FlightDeck"/> or <see cref="TrainingType.CabinCrew"/>.
    /// Must match the parent <see cref="Course.TrainingType"/> when attaching to a Module.
    /// </summary>
    public TrainingType TrainingType { get; set; }

    /// <summary>
    /// Suggested minimum duration in minutes. Copied into
    /// <see cref="Lesson.MinDurationMins"/> on attach (overridable per attachment).
    /// </summary>
    public int DefaultMinDurationMins { get; set; }

    /// <summary>
    /// Default value copied into <see cref="Lesson.RequiresInstructor"/> on attach.
    /// </summary>
    public bool RequiresInstructor { get; set; } = true;

    /// <summary>
    /// Default value copied into <see cref="Lesson.IsMandatory"/> on attach.
    /// </summary>
    public bool IsMandatoryByDefault { get; set; } = true;

    /// <summary>
    /// Free-text categorisation used for library filters (e.g. "Emergencies",
    /// "Navigation"). Optional.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// <c>true</c> when the template is visible in the library picker.
    /// Setting to <c>false</c> retires the template without affecting
    /// previously-attached <see cref="Lesson"/>s.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }
}
