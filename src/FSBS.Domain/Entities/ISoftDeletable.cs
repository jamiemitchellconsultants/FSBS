namespace FSBS.Domain.Entities;

/// <summary>
/// Marks an entity as soft-deletable. Records are never physically removed;
/// instead <see cref="IsDeleted"/> is set to <c>true</c> and a global EF Core
/// query filter hides them from all ordinary queries.
/// </summary>
/// <remarks>
/// Retaining deleted rows satisfies the seven-year data-retention requirement
/// and preserves referential integrity for audit trails and invoices.
/// To query deleted records intentionally, call <c>IgnoreQueryFilters()</c> on
/// the <c>IQueryable</c>.
/// </remarks>
public interface ISoftDeletable
{
    /// <summary>
    /// <c>true</c> when the record has been logically deleted. Rows where this
    /// flag is set are excluded from all standard queries via a global filter.
    /// </summary>
    bool IsDeleted { get; set; }
}
