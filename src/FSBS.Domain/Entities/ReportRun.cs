using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// A single execution instance of a <see cref="Report"/> definition.
/// Processing is asynchronous: the run is created in <c>Queued</c> state,
/// picked up by the reporting worker ECS task, and transitions through
/// <c>Running → Completed | Failed</c>.
/// </summary>
/// <remarks>
/// This entity does not extend <see cref="AuditableEntity"/> because it has
/// no standard <c>CreatedBy</c>/<c>UpdatedBy</c> audit columns — only
/// <see cref="TriggeredBy"/>, <see cref="CreatedAt"/>, and <see cref="UpdatedAt"/>
/// are stored. The EF configuration maps these explicitly.
/// On completion the output file is written to the <c>fsbs-documents</c> S3
/// bucket; the key is stored in <see cref="OutputS3Key"/> and served to
/// authorised users exclusively via pre-signed URLs (never via CloudFront).
/// </remarks>
public class ReportRun : EntityBase
{
    /// <summary>The report definition that was executed.</summary>
    public Guid ReportId { get; set; }

    /// <summary><see cref="AppUser.Id"/> of the user who triggered the run.</summary>
    public Guid TriggeredBy { get; set; }

    /// <summary>
    /// Current processing state: <c>Queued → Running → Completed | Failed</c>.
    /// </summary>
    public ReportRunStatus Status { get; set; }

    /// <summary>UTC timestamp at which the run record was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC timestamp of the most recent status transition.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>UTC timestamp at which the worker began processing this run.</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>UTC timestamp at which the run reached <c>Completed</c> or <c>Failed</c> state.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// S3 object key within the <c>fsbs-documents</c> bucket for the generated
    /// report file. Populated only when <see cref="Status"/> is <c>Completed</c>.
    /// Access is granted via a pre-signed URL — the bucket is never served
    /// through CloudFront.
    /// </summary>
    public string? ResultS3Key { get; set; }

    /// <summary>
    /// Human-readable error message captured when <see cref="Status"/> is
    /// <c>Failed</c>. Displayed to the user who triggered the run.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Navigation to the report definition that was executed.</summary>
    public Report Report { get; set; } = null!;
}
