using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace FSBS.Cdk.Lambdas;

/// <summary>
/// Re-syncs Cognito group membership from Entra ID groups (Staff Pool only)
/// and overrides JWT claims with the canonical app_role and tenant_id.
/// Runs inside the VPC so it can reach RDS in the isolated subnets.
/// </summary>
public class TokenRefreshFunction : Function
{
    public TokenRefreshFunction(
        Construct scope,
        string id,
        IVpc vpc,
        ISecurityGroup securityGroup) : base(scope, id, new FunctionProps
    {
        Runtime        = Runtime.DOTNET_8,
        Handler        = "FSBS.Functions::FSBS.Functions.TokenRefresh.Function::FunctionHandler",
        Code           = FunctionsAsset.Code,
        Timeout        = Duration.Seconds(15),
        MemorySize     = 512,
        Description    = "Cognito Token Refresh: re-syncs Entra groups, overrides token claims",
        Vpc            = vpc,
        VpcSubnets     = new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED },
        SecurityGroups = [securityGroup]
    })
    { }
}
