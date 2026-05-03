using System.Security.Claims;
using FSBS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FSBS.Api.Auth;

/// <summary>
/// Reads the current user's identity from the active HTTP request's JWT claims.
/// Registered as a scoped service so each request gets its own instance.
/// </summary>
/// <remarks>
/// <para>
/// The <c>sub</c> claim is the Cognito user sub GUID. In a full implementation
/// <c>FsbsClaimsTransformation</c> would resolve this to the application's own
/// <c>AppUser.Id</c> and add it as a separate claim. Until that transform is in
/// place, <c>UserId</c> returns the Cognito sub parsed as a GUID.
/// </para>
/// <para>
/// Unauthenticated requests (e.g. the public registration endpoints) return
/// <c>Guid.Empty</c> / <c>false</c> without throwing, allowing
/// <c>FsbsDbContext</c> to be resolved from DI even when no user is logged in.
/// The audit interceptor skips stamping <c>CreatedBy</c>/<c>UpdatedBy</c> when
/// <see cref="UserId"/> is <c>Guid.Empty</c>.
/// </para>
/// </remarks>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    /// <inheritdoc/>
    public Guid UserId =>
        Guid.TryParse(User?.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    /// <inheritdoc/>
    public Guid TenantId =>
        Guid.TryParse(User?.FindFirstValue("tenant_id"), out var id) ? id : Guid.Empty;

    /// <inheritdoc/>
    public Guid? OrgId =>
        Guid.TryParse(User?.FindFirstValue("org_id"), out var id) ? id : null;

    /// <inheritdoc/>
    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;
}
