namespace FSBS.Application.Tests.Common;

/// <summary>
/// Minimal <see cref="ICurrentUser"/> stand-in for handler tests. Construct with
/// the role and IDs the test needs; properties are read-only after construction.
/// </summary>
public sealed class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(
        AppRole role = AppRole.PrivateCustomer,
        Guid? userId = null,
        Guid? tenantId = null,
        Guid? orgId = null)
    {
        Role = role;
        UserId = userId ?? Guid.NewGuid();
        TenantId = tenantId ?? Guid.NewGuid();
        OrgId = orgId;
    }

    public Guid UserId { get; }
    public Guid TenantId { get; }
    public Guid? OrgId { get; }
    public AppRole Role { get; }
    public bool IsAuthenticated => UserId != Guid.Empty;

    public static FakeCurrentUser InternalStudent(Guid? userId = null) =>
        new(AppRole.InternalStudent, userId);

    public static FakeCurrentUser SalesStaff(Guid? userId = null) =>
        new(AppRole.SalesStaff, userId);

    public static FakeCurrentUser CorporateManager(Guid orgId, Guid? userId = null) =>
        new(AppRole.CorporateManager, userId, orgId: orgId);
}
