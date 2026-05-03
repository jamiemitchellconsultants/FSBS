using Amazon.Lambda.CognitoEvents;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FSBS.Functions.Cognito;

/// <summary>
/// Cognito Pre Sign-up Lambda trigger for the <c>fsbs-customer-pool</c>.
/// Fires before Cognito creates the user record, giving this function the
/// opportunity to allow or block the registration.
/// </summary>
/// <remarks>
/// <b>Decision logic:</b>
/// <list type="bullet">
///   <item>
///     <c>custom:registration_type == "private"</c> — private customer
///     self-registration. Allowed unconditionally; no invitation token required.
///   </item>
///   <item>
///     Any other value (or attribute absent) — corporate invitation-based flow.
///     Blocked here until the invitation validation logic is implemented in a
///     future feature. Throwing from this handler causes Cognito to reject the
///     sign-up with a 400 error.
///   </item>
/// </list>
/// The CDK AppStack wires this function to the customer pool's
/// <c>PreSignUp</c> trigger. It must never be attached to the staff pool.
/// </remarks>
public sealed class PreSignUpHandler
{
    private const string RegistrationTypeAttribute = "custom:registration_type";
    private const string PrivateRegistrationType = "private";

    public CognitoPreSignupEvent FunctionHandler(CognitoPreSignupEvent cognitoEvent, ILambdaContext context)
    {
        context.Logger.LogInformation(
            "PreSignUp trigger for user {Username}, source {TriggerSource}",
            cognitoEvent.UserName,
            cognitoEvent.TriggerSource);

        cognitoEvent.Request.UserAttributes.TryGetValue(RegistrationTypeAttribute, out var registrationType);

        if (registrationType == PrivateRegistrationType)
        {
            context.Logger.LogInformation(
                "Allowing private customer registration for {Username}",
                cognitoEvent.UserName);

            // Auto-verify the email attribute so the user only needs to confirm
            // via the 6-digit code, not a separate email verification step.
            cognitoEvent.Response.AutoVerifyEmail = true;
            return cognitoEvent;
        }

        // Corporate invitation flow is not yet implemented. Block to prevent
        // unintended registrations until it is built.
        context.Logger.LogWarning(
            "Blocking registration for {Username}: registration_type '{Type}' is not permitted",
            cognitoEvent.UserName,
            registrationType ?? "<not set>");

        throw new Exception(
            "Registration requires a valid invitation. " +
            "Please use the invitation link sent to your email address.");
    }
}
