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

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>All execution runs for this report.</summary>
    public ICollection<ReportRun> Runs { get; set; } = [];
}
