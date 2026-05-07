using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;

namespace FSBS.Cdk.Stacks;

public class NetworkStackProps : StackProps
{
    public string? CloudFrontPrefixListId { get; init; }
}

public class NetworkStack : Stack
{
    public Vpc Vpc { get; }

    // Security groups exported for downstream stacks
    public SecurityGroup AlbSg { get; }
    public SecurityGroup ApiSg { get; }
    public SecurityGroup RdsSg { get; }
    public SecurityGroup RedisSg { get; }

    /// <summary>
    /// Shared security group for in-VPC Lambdas (Cognito triggers, DB grants
    /// custom resource). Permits egress and is granted ingress to RDS below.
    /// </summary>
    public SecurityGroup LambdaSg { get; }

    public NetworkStack(Construct scope, string id, NetworkStackProps props) : base(scope, id, props)
    {
        var cloudFrontPrefixListId = props.CloudFrontPrefixListId ?? ResolveCloudFrontPrefixListId();

        Vpc = new Vpc(this, "Vpc", new VpcProps
        {
            MaxAzs = 3,
            NatGateways = 3,
            SubnetConfiguration =
            [
                new SubnetConfiguration { Name = "Public",   SubnetType = SubnetType.PUBLIC,           CidrMask = 24 },
                new SubnetConfiguration { Name = "Private",  SubnetType = SubnetType.PRIVATE_WITH_EGRESS, CidrMask = 24 },
                new SubnetConfiguration { Name = "Isolated", SubnetType = SubnetType.PRIVATE_ISOLATED, CidrMask = 24 }
            ]
        });

        // ALB — accepts HTTPS from CloudFront managed prefix list only
        AlbSg = new SecurityGroup(this, "AlbSg", new SecurityGroupProps
        {
            Vpc = Vpc,
            Description = "ALB: inbound HTTPS from CloudFront prefix list",
            AllowAllOutbound = true
        });
        AlbSg.AddIngressRule(
            Peer.PrefixList(cloudFrontPrefixListId),
            Port.Tcp(443),
            "HTTPS from CloudFront"
        );

        // API Fargate tasks — inbound from ALB only
        ApiSg = new SecurityGroup(this, "ApiSg", new SecurityGroupProps
        {
            Vpc = Vpc,
            Description = "API Fargate tasks",
            AllowAllOutbound = true
        });
        ApiSg.AddIngressRule(AlbSg, Port.Tcp(8080), "From ALB");

        // Lambdas in-VPC — egress to RDS + AWS service endpoints
        LambdaSg = new SecurityGroup(this, "LambdaSg", new SecurityGroupProps
        {
            Vpc = Vpc,
            Description = "Cognito triggers + DB grants Custom Resource",
            AllowAllOutbound = true
        });

        // RDS — inbound from API tasks and in-VPC Lambdas
        RdsSg = new SecurityGroup(this, "RdsSg", new SecurityGroupProps
        {
            Vpc = Vpc,
            Description = "RDS PostgreSQL",
            AllowAllOutbound = false
        });
        RdsSg.AddIngressRule(ApiSg, Port.Tcp(5432), "From API tasks");
        RdsSg.AddIngressRule(LambdaSg, Port.Tcp(5432), "From in-VPC Lambdas");

        // ElastiCache Redis — inbound from API tasks only
        RedisSg = new SecurityGroup(this, "RedisSg", new SecurityGroupProps
        {
            Vpc = Vpc,
            Description = "ElastiCache Redis",
            AllowAllOutbound = false
        });
        RedisSg.AddIngressRule(ApiSg, Port.Tcp(6379), "From API tasks");
    }

    private string ResolveCloudFrontPrefixListId()
    {
        // Known value for eu-west-1. Other regions should pass an explicit
        // value via CDK context to avoid accidental region mismatches.
        if (Region == "eu-west-1")
            return "pl-93a247fa";

        throw new InvalidOperationException(
            $"CloudFront managed prefix list id is not configured for region '{Region}'. " +
            "Set context key 'cloudFrontPrefixListId' when running CDK.");
    }
}
