namespace FSBS.Domain.Entities;

/// <summary>
/// An immutable point-in-time snapshot of an <see cref="Organisation"/>'s
/// account transaction history, generated on demand by Management or SalesStaff.
/// </summary>
/// <remarks>
/// Unlike most entities this does not extend <see cref="AuditableEntity"/>
/// because it carries only generation metadata, not update-tracking columns.
/// The statement document is stored as a file in the <c>fsbs-documents</c> S3
/// bucket and served exclusively via pre-signed URLs.
/// </remarks>
public class AccountStatement : EntityBase
{
    /// <summary>The organisation this statement was generated for.</summary>
    public Guid OrgId { get; set; }

    /// <summary>UTC timestamp at which the statement was generated.</summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary><see cref="AppUser.Id"/> of the user who triggered statement generation.</summary>
    public Guid GeneratedBy { get; set; }

    /// <summary>The start of the period covered by this statement.</summary>
    public DateOnly PeriodStart { get; set; }

    /// <summary>The end of the period covered by this statement.</summary>
    public DateOnly PeriodEnd { get; set; }

    /// <summary>Account balance in GBP at the start of the statement period.</summary>
    public decimal OpeningBalanceGbp { get; set; }

    /// <summary>Account balance in GBP at the end of the statement period.</summary>
    public decimal ClosingBalanceGbp { get; set; }

    /// <summary>
    /// S3 object key for the generated statement PDF within the
    /// <c>fsbs-documents</c> bucket. Served via pre-signed URL only —
    /// never through CloudFront.
    /// </summary>
    public string StatementS3Key { get; set; } = string.Empty;

    /// <summary>Navigation to the organisation this statement belongs to.</summary>
    public Organisation Organisation { get; set; } = null!;
}
