using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;

namespace FSBS.Cdk.Stacks;

public class NetworkStackProps : StackProps { }

public class NetworkStack : Stack
{
    public Vpc Vpc { get; }

    // Security groups exported for downstream stacks
    public SecurityGroup AlbSg { get; }
    public SecurityGroup ApiSg { get; }
    public SecurityGroup RdsSg { get; }
    public SecurityGroup RedisSg { get; }

    public NetworkStack(Construct scope, string id, NetworkStackProps props) : base(scope, id, props)
    {
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
            AllowAllOutbound = false
        });
        AlbSg.AddIngressRule(
            Peer.PrefixList("pl-93a247fa"), // CloudFront managed prefix list (eu-west-1)
            Port.Tcp(443),
            "HTTPS from CloudFront"
        );
        AlbSg.AddEgressRule(Peer.AnyIpv4(), Port.AllTraffic(), "Allow all outbound");

        // API Fargate tasks — inbound from ALB only
        ApiSg = new SecurityGroup(this, "ApiSg", new SecurityGroupProps
        {
            Vpc = Vpc,
            Description = "API Fargate tasks",
            AllowAllOutbound = true
        });
        ApiSg.AddIngressRule(AlbSg, Port.Tcp(8080), "From ALB");

        // RDS — inbound from API tasks only
        RdsSg = new SecurityGroup(this, "RdsSg", new SecurityGroupProps
        {
            Vpc = Vpc,
            Description = "RDS PostgreSQL",
            AllowAllOutbound = false
        });
        RdsSg.AddIngressRule(ApiSg, Port.Tcp(5432), "From API tasks");

        // ElastiCache Redis — inbound from API tasks only
        RedisSg = new SecurityGroup(this, "RedisSg", new SecurityGroupProps
        {
            Vpc = Vpc,
            Description = "ElastiCache Redis",
            AllowAllOutbound = false
        });
        RedisSg.AddIngressRule(ApiSg, Port.Tcp(6379), "From API tasks");
    }
}
