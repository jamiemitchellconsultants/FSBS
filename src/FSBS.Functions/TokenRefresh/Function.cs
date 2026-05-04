using Amazon.Lambda.CognitoEvents;
using Amazon.Lambda.Core;


namespace FSBS.Functions.TokenRefresh;

/// <summary>
/// Cognito Pre Token Generation Lambda trigger for the <c>fsbs-staff-pool</c>.
/// Fires before Cognito generates ID and access tokens.
/// </summary>
/// <remarks>
/// <b>Responsibilities:</b>
/// <list type="bullet">
///   <item>Re-syncs Cognito group membership from Entra ID groups.</item>
///   <item>Calls <c>AdminUserGlobalSignOut</c> when Entra account is disabled.</item>
/// </list>
/// The CDK AppStack wires this function to the staff pool's
/// <c>PreTokenGeneration</c> trigger. It must never be attached to the customer pool.
/// </remarks>
public sealed class Function
{
    public CognitoPreTokenGenerationEvent FunctionHandler(CognitoPreTokenGenerationEvent cognitoEvent, ILambdaContext context)
    {
        context.Logger.LogInformation(
            "PreTokenGeneration trigger for user {Username}, pool {UserPoolId}, source {TriggerSource}",
            cognitoEvent.UserName,
            cognitoEvent.UserPoolId,
            cognitoEvent.TriggerSource);

        // TODO: Implement logic for re-syncing Cognito group membership from Entra ID groups
        // and calling AdminUserGlobalSignOut when Entra account is disabled.

        context.Logger.LogInformation("PreTokenGeneration processing complete for user {Username}", cognitoEvent.UserName);

        return cognitoEvent;
    }
}
