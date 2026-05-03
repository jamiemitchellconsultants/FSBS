using System.Net;
using System.Net.Http.Json;

namespace FSBS.Web.Services;

public sealed class InvitationService(HttpClient http)
{
    public async Task<InvitationValidationResult?> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        var response = await http.GetAsync(
            $"v1/invitations/validate?token={Uri.EscapeDataString(token)}", ct);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<InvitationValidationResult>(ct)
               ?? new InvitationValidationResult(false, null, null, null);
    }

    public async Task<IssueInvitationResult> InviteStudentAsync(
        string inviteeEmail,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(
            "v1/invitations/students",
            new { inviteeEmail },
            ct);

        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new InvalidOperationException(
                "A pending invitation for this email address already exists in your organisation.");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IssueInvitationResult>(ct)
            ?? throw new InvalidOperationException("Unexpected empty response from server.");
    }

    public async Task<IssueInvitationResult> IssueInvitationAsync(
        string inviteeEmail,
        Guid orgId,
        string role,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(
            "v1/invitations",
            new { inviteeEmail, orgId },
            ct);

        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new InvalidOperationException(
                "A pending invitation for this email address already exists for the selected organisation.");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IssueInvitationResult>(ct)
            ?? throw new InvalidOperationException("Unexpected empty response from server.");
    }
}

public record InvitationValidationResult(bool IsValid, string? InviteeEmail, string? OrgName, string? Role);

public record IssueInvitationResult(
    Guid InvitationId,
    string InviteeEmail,
    string OrgName,
    DateTimeOffset ExpiresAt);
