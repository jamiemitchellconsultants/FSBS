namespace FSBS.Domain.Entities;

/// <summary>
/// Marks an entity as belonging to a specific tenant. Applied to
/// <see cref="AppUser"/>, <see cref="Organisation"/>, and <see cref="Course"/>.
/// </summary>
/// <remarks>
/// A global EF Core query filter restricts every query to rows whose
/// <see cref="TenantId"/> matches the value from the current request's JWT
/// (<c>ICurrentUser.TenantId</c>). Staff users always operate under the
/// school's root tenant ID so they can see all customer data.
/// PostgreSQL row-level security enforces the same isolation at the storage
/// layer via <c>SET LOCAL app.current_tenant_id</c>.
/// </remarks>
public interface ITenantScoped
{
    /// <summary>
    /// Identifier of the tenant that owns this record. Derived from the
    /// <c>tenant_id</c> claim in the authenticated user's JWT and injected
    /// into <c>DbContext</c> by middleware before any handler executes.
    /// </summary>
    Guid TenantId { get; set; }
}
