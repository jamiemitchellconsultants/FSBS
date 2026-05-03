namespace FSBS.Infrastructure.Cognito;

/// <summary>
/// Bound from <c>appsettings.json</c> section <c>"Cognito"</c>. In deployed
/// environments the values are injected as environment variables by the CDK
/// AppStack from Secrets Manager / SSM Parameter Store.
/// </summary>
public sealed class CognitoSettings
{
    /// <summary>AWS region the Cognito pools are deployed in (e.g. <c>eu-west-1</c>).</summary>
    public string Region { get; init; } = "eu-west-1";

    /// <summary>
    /// App client ID for the customer pool (<c>fsbs-customer-pool</c>).
    /// This is the client used for self-registration and sign-in flows.
    /// Must be a public client (no client secret) when called from the API.
    /// </summary>
    public string CustomerPoolClientId { get; init; } = string.Empty;

    /// <summary>
    /// User Pool ID for the customer pool. Used for admin operations that
    /// require the pool ID rather than the client ID.
    /// </summary>
    public string CustomerPoolId { get; init; } = string.Empty;
}
