using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace FSBS.Cdk.Lambdas;

/// <summary>
/// Creates AppUser record, assigns org_id/app_role, marks invitation Claimed,
/// and places staff users in the correct Cognito group.
/// Runs inside the VPC so it can reach RDS in the isolated subnets.
/// </summary>
public class PostConfirmationFunction : Function
{
    public PostConfirmationFunction(
        Construct scope,
        string id,
        IVpc vpc,
        ISecurityGroup securityGroup) : base(scope, id, new FunctionProps
    {
        Runtime        = Runtime.DOTNET_8,
        Handler        = "FSBS.Functions::FSBS.Functions.PostConfirmation.Function::FunctionHandler",
        Code           = FunctionsAsset.Code,
        Timeout        = Duration.Seconds(20),
        MemorySize     = 512,
        Description    = "Cognito Post Confirmation: creates AppUser, assigns role, marks invitation Claimed",
        Vpc            = vpc,
        VpcSubnets     = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
        SecurityGroups = [securityGroup]
    })
    { }
}
