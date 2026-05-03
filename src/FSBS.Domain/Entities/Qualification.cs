namespace FSBS.Domain.Entities;

/// <summary>
/// A named flight or cabin-crew qualification (e.g. ATPL, Type Rating, SEP).
/// Used as reference data when recording instructor competencies and flagging
/// upcoming expiries for regulatory compliance.
/// </summary>
/// <remarks>
/// The <c>QualificationExpiring</c> notification event is dispatched 30 and 7 days
/// before a linked qualification's expiry date so that the relevant instructor
/// or course director can arrange renewal.
/// </remarks>
public class Qualification : AuditableEntity, ISoftDeletable
{
    /// <summary>
    /// Short, human-readable qualification name displayed in the UI and
    /// on instructor profiles (e.g. "ATPL(A)", "B737 Type Rating").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional longer description of the qualification's scope, issuing
    /// authority, or renewal requirements.
    /// </summary>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }
}
