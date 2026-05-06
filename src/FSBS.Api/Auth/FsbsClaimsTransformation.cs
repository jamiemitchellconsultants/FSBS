using System.Security.Claims;
using FSBS.Domain.Enums;
using Microsoft.AspNetCore.Authentication;

namespace FSBS.Api.Auth;

/// <summary>
/// Normalises claims from either Cognito pool's JWT into a unified set of
/// application claims that the rest of the API can rely on regardless of
/// which pool (Staff or Customer) issued the token.
/// </summary>
/// <remarks>
/// <para>
/// Both pools emit <c>app_role</c> and <c>tenant_id</c> as custom Cognito
/// attributes. The Staff pool additionally emits <c>custom:entra_groups</c>
/// (used only by the Token Refresh Lambda; not consumed here). The Customer
/// pool emits <c>org_id</c> for CorporateManager and CorporateStudent roles.
/// </para>
/// <para>
/// This transform is idempotent — if the normalised claims are already present
/// (e.g. on a second call within the same request) they are not duplicated.
/// </para>
/// </remarks>
public sealed class FsbsClaimsTransformation : IClaimsTransformation
{
    // Cognito stores custom attributes under the "custom:" namespace when
    // accessed via the user-info endpoint, but the JWT itself surfaces them
    // without the prefix for attributes mapped in the pool's token claims.
    // We check both forms so the transform works with either mapping style.
    private static readonly string[] AppRoleClaimNames   = ["app_role",   "custom:app_role"];
    private static readonly string[] TenantIdClaimNames  = ["tenant_id",  "custom:tenant_id"];
    private static readonly string[] OrgIdClaimNames     = ["org_id",     "custom:org_id"];

    /// <inheritdoc/>
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Only transform authenticated principals.
        if (principal.Identity is not { IsAuthenticated: true })
            return Task.FromResult(principal);

        // Avoid mutating the original identity — clone into a new one.
        var identity = new ClaimsIdentity(
            principal.Identity,
            claims: null,
            authenticationType: principal.Identity.AuthenticationType,
            nameType: principal.Identity.Name,
            roleType: ClaimTypes.Role);

        NormaliseAppRole(principal, identity);
        NormaliseTenantId(principal, identity);
        NormaliseOrgId(principal, identity);

        return Task.FromResult(new ClaimsPrincipal(identity));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Ensures a canonical <c>app_role</c> claim is present and that the value
    /// is a valid <see cref="AppRole"/> enum member. Falls back to
    /// <see cref="AppRole.PrivateCustomer"/> when the claim is absent or
    /// unrecognised (e.g. a Customer pool token with no explicit role set).
    /// </summary>
    private static void NormaliseAppRole(ClaimsPrincipal source, ClaimsIdentity target)
    {
        // Skip if already normalised by a previous transform call.
        if (target.HasClaim(c => c.Type == "app_role"))
            return;

        var raw = FindFirstValue(source, AppRoleClaimNames);

        var role = Enum.TryParse<AppRole>(raw, ignoreCase: true, out var parsed)
            ? parsed
            : AppRole.PrivateCustomer;

        target.AddClaim(new Claim("app_role", role.ToString()));

        // Also add as the standard ClaimTypes.Role so [Authorize(Roles = ...)]
        // works alongside the named policy approach.
        target.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));
    }

    /// <summary>
    /// Ensures a canonical <c>tenant_id</c> claim is present.
    /// Staff tokens always carry the school's root tenant GUID.
    /// Customer tokens carry the customer's own tenant GUID.
    /// </summary>
    private static void NormaliseTenantId(ClaimsPrincipal source, ClaimsIdentity target)
    {
        if (target.HasClaim(c => c.Type == "tenant_id"))
            return;

        var raw = FindFirstValue(source, TenantIdClaimNames);

        if (!string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out var tenantId))
            target.AddClaim(new Claim("tenant_id", tenantId.ToString()));
    }

    /// <summary>
    /// Ensures a canonical <c>org_id</c> claim is present when the token
    /// carries one. Only CorporateManager and CorporateStudent tokens include
    /// this attribute; it is absent for Staff and PrivateCustomer tokens.
    /// </summary>
    private static void NormaliseOrgId(ClaimsPrincipal source, ClaimsIdentity target)
    {
        if (target.HasClaim(c => c.Type == "org_id"))
            return;

        var raw = FindFirstValue(source, OrgIdClaimNames);

        if (!string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out var orgId))
            target.AddClaim(new Claim("org_id", orgId.ToString()));
    }

    /// <summary>
    /// Returns the first non-null value found for any of the supplied claim
    /// type names, or <c>null</c> if none are present.
    /// </summary>
    private static string? FindFirstValue(ClaimsPrincipal principal, string[] claimTypes)
    {
        foreach (var type in claimTypes)
        {
            var value = principal.FindFirstValue(type);
            if (value is not null)
                return value;
        }

        return null;
    }
}
