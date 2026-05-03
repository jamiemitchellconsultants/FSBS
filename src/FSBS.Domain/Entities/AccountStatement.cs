namespace FSBS.Domain.Entities;

/// <summary>
/// An immutable point-in-time snapshot of an <see cref="OrgAccount"/>'s
/// transaction history, generated on demand by Management or SalesStaff.
/// </summary>
/// <remarks>
/// Unlike most entities this does not extend <see cref="AuditableEntity"/>
/// because it carries only generation metadata, not update-tracking columns.
/// The statement content is stored as a JSON document in
/// <see cref="StatementJson"/> so that the exact figures presented to the
/// customer are preserved regardless of subsequent data changes.
/// </remarks>
public class AccountStatement : EntityBase
{
    /// <summary>The account this statement was generated for.</summary>
    public Guid OrgAccountId { get; set; }

    /// <summary>UTC timestamp at which the statement was generated.</summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary><see cref="AppUser.Id"/> of the user who triggered statement generation.</summary>
    public Guid GeneratedBy { get; set; }

    /// <summary>
    /// JSON document containing the full statement detail — opening balance,
    /// itemised invoices, payments, and closing balance — as it stood at
    /// <see cref="GeneratedAt"/>. Stored as <c>jsonb</c> in PostgreSQL.
    /// </summary>
    public string StatementJson { get; set; } = string.Empty;

    /// <summary>Navigation to the account this statement belongs to.</summary>
    public OrgAccount OrgAccount { get; set; } = null!;
}
