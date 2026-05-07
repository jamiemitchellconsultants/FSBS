using System.Security.Claims;
using Fluxor;
using Microsoft.AspNetCore.Components.Authorization;
using FSBS.Web.Services;
using FSBS.Web.State.Session;

namespace FSBS.Web.Auth;

/// <summary>
/// Resolves authentication state by calling GET /v1/auth/me on every load.
/// In development the stored Bearer token is attached by AuthService.
/// In production the HttpOnly cookie is sent automatically by the browser.
/// Dispatches SetSessionAction so Fluxor SessionState stays in sync.
/// </summary>
public sealed class CognitoAuthStateProvider(
    AuthService authService,
    IDispatcher dispatcher) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var me = await authService.GetMeAsync();
        if (me is null)
        {
            dispatcher.Dispatch(new ClearSessionAction());
            return Anonymous;
        }

        var identity = BuildIdentity(me);
        var principal = new ClaimsPrincipal(identity);

        dispatcher.Dispatch(new SetSessionAction(
            UserId:   Guid.TryParse(me.Sub,      out var uid) ? uid : Guid.Empty,
            TenantId: Guid.TryParse(me.TenantId, out var tid) ? tid : Guid.Empty,
            AppRole:  me.AppRole,
            OrgId:    me.OrgId));

        return new AuthenticationState(principal);
    }

    /// <summary>
    /// Call after login or logout to trigger a re-evaluation of auth state
    /// across all AuthorizeView components.
    /// </summary>
    public void NotifyAuthChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static ClaimsIdentity BuildIdentity(MeResponse me)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, me.Sub),
            new(ClaimTypes.Email,          me.Email),
            new(ClaimTypes.Name,           string.IsNullOrWhiteSpace(me.Name) ? me.Email : me.Name),
            new(ClaimTypes.Role,           me.AppRole),
            new("app_role",                me.AppRole),
            new("tenant_id",               me.TenantId),
        };

        if (!string.IsNullOrWhiteSpace(me.OrgId))
            claims.Add(new Claim("org_id", me.OrgId));

        return new ClaimsIdentity(claims, authenticationType: "Cognito");
    }
}
