using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace FSBS.Web.Auth;

/// <summary>
/// Stub authentication state provider that returns an anonymous (unauthenticated) principal.
/// Replaced in a later step with the real Cognito OIDC provider.
/// </summary>
public sealed class AnonymousAuthStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(Anonymous);
}
