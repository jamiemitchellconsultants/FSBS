using System.Net.Http.Json;

namespace FSBS.Web.Services;

public sealed class AuthService(HttpClient http)
{
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
}
