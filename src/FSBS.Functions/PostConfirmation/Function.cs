using Amazon.Lambda.CognitoEvents;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FSBS.Functions.PostConfirmation;

/// <summary>
/// Cognito Post Confirmation Lambda trigger.
/// Fires after a user has successfully confirmed their account.
/// </summary>
/// <remarks>
/// <b>Responsibilities:</b>
/// <list type="bullet">
///   <item>Creates an <c>AppUser</c> record in the database.</item>
///   <item>Assigns <c>org_id</c> and <c>app_role</c> for customer users.</item>
///   <item>Marks invitation as <c>Claimed</c> for invitation-based registrations.</item>
///   <item>Places staff users in the matching Cognito group based on Entra ID groups.</item>
/// </list>
/// The CDK AppStack wires this function to both the staff and customer pool's
/// <c>PostConfirmation</c> trigger.
/// </remarks>
public sealed class Function
{
    public CognitoPostConfirmationEvent FunctionHandler(CognitoPostConfirmationEvent cognitoEvent, ILambdaContext context)
    {
        context.Logger.LogInformation(
            "PostConfirmation trigger for user {Username}, pool {UserPoolId}, source {TriggerSource}",
            cognitoEvent.UserName,
            cognitoEvent.UserPoolId,
            cognitoEvent.TriggerSource);

        // TODO: Implement logic for creating AppUser record, assigning org_id/app_role,
        // marking invitation as Claimed, and placing staff in Cognito groups.

        context.Logger.LogInformation("PostConfirmation processing complete for user {Username}", cognitoEvent.UserName);

        return cognitoEvent;
    }
}
