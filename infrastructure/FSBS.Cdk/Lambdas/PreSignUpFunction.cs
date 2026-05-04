using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.IAM;
using Constructs;

namespace FSBS.Cdk.Lambdas;

/// <summary>
/// Validates the SHA-256 invitation token hash before allowing Customer Pool sign-up.
/// Rejects any registration without a valid Pending invitation.
/// </summary>
public class PreSignUpFunction : Function
{
    public PreSignUpFunction(Construct scope, string id) : base(scope, id, new FunctionProps
    {
        Runtime = Runtime.DOTNET_8,
        Handler = "FSBS.Functions::FSBS.Functions.PreSignUp.Function::FunctionHandler",
        Code = Code.FromAsset("src/FSBS.Functions/PreSignUp/publish"),
        Timeout = Duration.Seconds(10),
        Description = "Cognito Pre Sign-up: validates invitation token hash"
    })
    {
        // Needs read access to the invitations table via the API or direct DB — grant is applied in AppStack
    }
}
