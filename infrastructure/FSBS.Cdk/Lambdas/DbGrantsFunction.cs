using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace FSBS.Cdk.Lambdas;

/// <summary>
/// Lambda function that provisions the <c>fsbs_app</c> and
/// <c>fsbs_readonly</c> roles inside the RDS PostgreSQL instance. Invoked
/// once per stack deployment via a CloudFormation Custom Resource backed by
/// the CDK Provider framework.
/// </summary>
/// <remarks>
/// Runs inside the VPC isolated subnet so it can reach RDS. The function
/// itself is idempotent — safe to re-run on stack update.
/// </remarks>
public class DbGrantsFunction : Function
{
    public DbGrantsFunction(
        Construct scope,
        string id,
        IVpc vpc,
        ISecurityGroup securityGroup) : base(scope, id, new FunctionProps
    {
        Runtime     = Runtime.DOTNET_8,
        Handler     = "FSBS.Functions::FSBS.Functions.DbGrants.Function::FunctionHandler",
        Code        = FunctionsAsset.Code,
        Timeout     = Duration.Minutes(15),
        MemorySize  = 1024,
        Description = "Provisions fsbs_app and fsbs_readonly DB roles via Custom Resource",
        Vpc         = vpc,
        VpcSubnets  = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
        SecurityGroups = [securityGroup]
    })
    { }
}
