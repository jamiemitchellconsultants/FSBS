using System.Net.Http.Json;
using Blazored.LocalStorage;

namespace FSBS.Web.Services;

public sealed class AuthService(HttpClient http, ILocalStorageService localStorage)
{
    private const string TokenKey = "fsbs_token";

    // ── Registration ──────────────────────────────────────────────────────────

    public async Task RegisterAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string? phoneNumber,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("v1/auth/register", new
        {
            email,
            password,
            firstName,
            lastName,
            phoneNumber
        }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ConfirmAsync(string email, string confirmationCode, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("v1/auth/register/confirm", new
        {
            email,
            confirmationCode
        }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ResendCodeAsync(string email, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("v1/auth/register/resend-code", new { email }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RegisterInvitedAsync(
        string token,
        string password,
        string firstName,
        string lastName,
        string? phoneNumber,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("v1/auth/register/invited", new
        {
            token,
            password,
            firstName,
            lastName,
            phoneNumber,
        }, ct);
        response.EnsureSuccessStatusCode();
    }

    // ── Session ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current user's identity from the API.
    /// In dev the Bearer token is read from localStorage and sent as a header.
    /// In production the HttpOnly cookie is sent automatically by the browser.
    /// Returns null when unauthenticated.
    /// </summary>
    public async Task<MeResponse?> GetMeAsync(CancellationToken ct = default)
    {
        try
        // amazonq-ignore-next-line
        {
            var token = await localStorage.GetItemAsync<string>(TokenKey, ct);

            using var request = new HttpRequestMessage(HttpMethod.Get, "v1/auth/me");
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<MeResponse>(cancellationToken: ct);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Dev-only: exchanges role/email for a signed JWT from POST /dev/auth/token
    /// and stores it in localStorage so GetMeAsync can attach it as a Bearer token.
    /// </summary>
    public async Task<string?> GetDevTokenAsync(
        string role = "PrivateCustomer",
        string email = "dev@fsbs.local",
        string? orgId = null,
        CancellationToken ct = default)
    {
        var url = $"dev/auth/token?email={Uri.EscapeDataString(email)}&role={Uri.EscapeDataString(role)}";
        if (orgId is not null)
            url += $"&orgId={Uri.EscapeDataString(orgId)}";

        var response = await http.PostAsync(url, null, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DevTokenResponse>(cancellationToken: ct);
        if (result?.Token is null)
            return null;

        await localStorage.SetItemAsync(TokenKey, result.Token, ct);
        return result.Token;
    }

    /// <summary>
    /// Clears the local token and calls the API logout endpoint to clear the session cookie.
    /// </summary>
    public async Task SignOutAsync(CancellationToken ct = default)
    {
        await localStorage.RemoveItemAsync(TokenKey, ct);
        try
        {
            await http.PostAsync("v1/auth/logout", null, ct);
        }
        catch
        {
            // Best-effort — local token is already cleared.
        }
    }

    public async Task StoreTokenAsync(string token, CancellationToken ct = default) =>
        await localStorage.SetItemAsync(TokenKey, token, ct);

    // amazonq-ignore-next-line
    public async Task<string?> GetStoredTokenAsync(CancellationToken ct = default) =>
        await localStorage.GetItemAsync<string>(TokenKey, ct);
}

public sealed record MeResponse(
    string Sub,
    string Email,
    string Name,
    string AppRole,
    string TenantId,
    string? OrgId);

internal sealed record DevTokenResponse(string Token, string UserId, string TenantId, string Email, string Role);
