namespace FSBS.Domain.Entities;

/// <summary>
/// A named management report definition. The report's query, filters, and
/// output format are described in <see cref="DefinitionJson"/> and executed
/// on demand by creating a <see cref="ReportRun"/>.
/// </summary>
/// <remarks>
/// Reports are typically created and maintained by SystemAdmin or Management
/// users via the reporting configuration UI. Each execution produces a
/// <see cref="ReportRun"/> that is processed asynchronously by the reporting
/// worker and stored as a file in S3 (<c>fsbs-documents</c> bucket), accessible
/// via a pre-signed URL.
/// </remarks>
public class Report : AuditableEntity, ISoftDeletable
{
    /// <summary>Human-readable report name displayed in the management dashboard.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description of the report's purpose and intended audience.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// JSON document describing the report's data source, filters, columns,
    /// groupings, and output format. Stored as a <c>jsonb</c> column.
    /// Interpreted by the reporting service when a run is triggered.
    /// </summary>
    public string DefinitionJson { get; set; } = string.Empty;

    /// <summary><see cref="AppUser.Id"/> of the user who created and owns this report definition.</summary>
    public Guid OwnerId { get; set; }

    /// <summary>Whether this report is visible to all Management/SystemAdmin users or only the owner.</summary>
    public bool IsShared { get; set; }

    /// <summary>
    /// Optional cron expression for scheduled automatic execution (e.g. <c>"0 6 * * 1"</c>).
    /// <c>null</c> means the report is run on demand only.
    /// </summary>
    public string? ScheduleCron { get; set; }

    /// <summary>UTC timestamp of the most recent completed run. <c>null</c> if never run.</summary>
    public DateTimeOffset? LastRunAt { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>All execution runs for this report.</summary>
    public ICollection<ReportRun> Runs { get; set; } = [];
}
