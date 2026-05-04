using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace FSBS.Cdk.Lambdas;

/// <summary>
/// Creates AppUser record, assigns org_id/app_role, marks invitation Claimed,
/// and places staff users in the correct Cognito group.
/// </summary>
public class PostConfirmationFunction : Function
{
    public PostConfirmationFunction(Construct scope, string id) : base(scope, id, new FunctionProps
    {
        Runtime = Runtime.DOTNET_8,
        Handler = "FSBS.Functions::FSBS.Functions.PostConfirmation.Function::FunctionHandler",
        Code = Code.FromAsset("src/FSBS.Functions/PostConfirmation/publish"),
        Timeout = Duration.Seconds(15),
        Description = "Cognito Post Confirmation: creates AppUser, assigns role, marks invitation Claimed"
    })
    { }
}
