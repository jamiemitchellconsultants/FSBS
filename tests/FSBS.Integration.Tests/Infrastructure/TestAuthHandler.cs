using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSBS.Integration.Tests.Infrastructure;

/// <summary>
/// Authentication handler used in tests. Reads <c>X-Test-Role</c>,
/// <c>X-Test-User</c>, <c>X-Test-Tenant</c>, and <c>X-Test-Org</c> headers and
/// emits the same claim shape Cognito would produce, so the real
/// <c>FsbsClaimsTransformation</c> normalises them as in production.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    public const string RoleHeader = "X-Test-Role";
    public const string UserHeader = "X-Test-User";
    public const string TenantHeader = "X-Test-Tenant";
    public const string OrgHeader = "X-Test-Org";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var roleValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var role = roleValues.ToString();
        var userId = Request.Headers.TryGetValue(UserHeader, out var u) && Guid.TryParse(u, out var uid)
            ? uid : Guid.NewGuid();
        var tenantId = Request.Headers.TryGetValue(TenantHeader, out var t) && Guid.TryParse(t, out var tid)
            ? tid : Guid.NewGuid();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("sub", userId.ToString()),
            new("app_role", role),
            new("tenant_id", tenantId.ToString()),
        };

        if (Request.Headers.TryGetValue(OrgHeader, out var o) && Guid.TryParse(o, out var oid))
            claims.Add(new Claim("org_id", oid.ToString()));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
