using Fluxor;

namespace FSBS.Web.State.Session;

[FeatureState]
public record SessionState
{
    public Guid UserId { get; init; } = Guid.Empty;
    public Guid TenantId { get; init; } = Guid.Empty;
    public string AppRole { get; init; } = string.Empty;
    public string? OrgId { get; init; }
    public bool IsAuthenticated { get; init; }
}
