namespace FSBS.Application.Common.Interfaces;

/// <summary>
/// Handles Cognito Hosted UI callback token exchange for auth code flows.
/// </summary>
public interface ICognitoHostedUiService
{
    Task<CognitoHostedUiCallbackResult> ProcessCallbackAsync(
        string? code,
        string? state,
        string? error,
        CancellationToken ct = default);
}

public sealed record CognitoHostedUiCallbackResult(
    bool Success,
    string? ErrorCode,
    string? IdToken,
    string? RefreshToken,
    int ExpiresInSeconds);

