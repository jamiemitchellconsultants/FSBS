using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace FSBS.Cdk.Lambdas;

/// <summary>
/// Validates the SHA-256 invitation token hash before allowing Customer Pool sign-up.
/// Rejects any registration without a valid Pending invitation.
/// Runs inside the VPC so it can reach RDS in the isolated subnets.
/// </summary>
public class PreSignUpFunction : Function
{
    public PreSignUpFunction(
        Construct scope,
        string id,
        IVpc vpc,
        ISecurityGroup securityGroup) : base(scope, id, new FunctionProps
    {
        Runtime        = Runtime.DOTNET_8,
        Handler        = "FSBS.Functions::FSBS.Functions.PreSignUp.Function::FunctionHandler",
        Code           = FunctionsAsset.Code,
        Timeout        = Duration.Seconds(15),
        MemorySize     = 512,
        Description    = "Cognito Pre Sign-up: validates invitation token hash",
        Vpc            = vpc,
        VpcSubnets     = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
        SecurityGroups = [securityGroup]
    })
    { }
}
