using System.Text.Json;
using FSBS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FSBS.Infrastructure.Cognito;

public sealed class CognitoHostedUiService(
    IConfiguration config,
    IHttpClientFactory httpClientFactory)
    : ICognitoHostedUiService
{
    public async Task<CognitoHostedUiCallbackResult> ProcessCallbackAsync(
        string? code,
        string? state,
        string? error,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(error))
            return new CognitoHostedUiCallbackResult(false, error, null, null, 0);

        if (string.IsNullOrWhiteSpace(code))
            return new CognitoHostedUiCallbackResult(false, "missing_code", null, null, 0);

        var isStaff = state?.StartsWith("staff|", StringComparison.OrdinalIgnoreCase) ?? false;

        var awsRegion = config["Cognito:AwsRegion"] ?? string.Empty;
        var poolId = isStaff ? config["Cognito:StaffPoolId"] : config["Cognito:CustomerPoolId"];
        var clientId = isStaff ? config["Cognito:StaffClientId"] : config["Cognito:CustomerClientId"];
        var clientSecret = isStaff ? config["Cognito:StaffClientSecret"] : config["Cognito:CustomerClientSecret"];
        var redirectUri = isStaff ? config["Cognito:StaffCallbackUrl"] : config["Cognito:CustomerCallbackUrl"];
        var domain = config["Cognito:Domain"];

        if (string.IsNullOrWhiteSpace(poolId)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(redirectUri)
            || string.IsNullOrWhiteSpace(domain)
            || string.IsNullOrWhiteSpace(awsRegion))
        {
            return new CognitoHostedUiCallbackResult(false, "misconfigured", null, null, 0);
        }

        var tokenEndpoint = $"https://{domain}.auth.{awsRegion}.amazoncognito.com/oauth2/token";

        var formValues = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
        };

        if (!string.IsNullOrWhiteSpace(clientSecret))
            formValues["client_secret"] = clientSecret;

        using var client = httpClientFactory.CreateClient();
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(formValues)
        };

        HttpResponseMessage tokenResponse;
        try
        {
            tokenResponse = await client.SendAsync(tokenRequest, ct);
        }
        catch
        {
            return new CognitoHostedUiCallbackResult(false, "token_exchange_failed", null, null, 0);
        }

        if (!tokenResponse.IsSuccessStatusCode)
            return new CognitoHostedUiCallbackResult(false, "token_exchange_failed", null, null, 0);

        var json = await tokenResponse.Content.ReadAsStringAsync(ct);

        try
        {
            using var doc = JsonDocument.Parse(json);

            var idToken = doc.RootElement.TryGetProperty("id_token", out var idTok)
                ? idTok.GetString()
                : null;

            var refreshToken = doc.RootElement.TryGetProperty("refresh_token", out var refTok)
                ? refTok.GetString()
                : null;

            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var expIn)
                ? expIn.GetInt32()
                : 3600;

            if (string.IsNullOrWhiteSpace(idToken))
                return new CognitoHostedUiCallbackResult(false, "no_id_token", null, null, 0);

            return new CognitoHostedUiCallbackResult(true, null, idToken, refreshToken, expiresIn);
        }
        catch
        {
            return new CognitoHostedUiCallbackResult(false, "token_exchange_failed", null, null, 0);
        }
    }
}

