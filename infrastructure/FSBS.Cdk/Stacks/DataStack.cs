using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ElastiCache;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SecretsManager;
using Constructs;
using InstanceType = Amazon.CDK.AWS.EC2.InstanceType;

namespace FSBS.Cdk.Stacks;

public class DataStackProps : StackProps
{
    public required NetworkStack Network { get; init; }
    public required string DeployEnv { get; init; }
}

public class DataStack : Stack
{
    public Secret DbSecret { get; }
    public CfnReplicationGroup RedisCluster { get; }
    public Bucket StaticBucket { get; }
    public Bucket DocumentsBucket { get; }

    public DataStack(Construct scope, string id, DataStackProps props) : base(scope, id, props)
    {
        var net = props.Network;
        var isProd = props.DeployEnv == "production";

        // ── RDS credentials ──────────────────────────────────────────────────
        DbSecret = new Secret(this, "DbSecret", new SecretProps
        {
            SecretName = "fsbs/rds/credentials",
            GenerateSecretString = new SecretStringGenerator
            {
                SecretStringTemplate = "{\"username\":\"fsbs_app\"}",
                GenerateStringKey = "password",
                ExcludeCharacters = "/@\" "
            }
        });

        // 30-day rotation via Secrets Manager hosted rotation (single-user)
        _ = new RotationSchedule(this, "DbSecretRotation", new RotationScheduleProps
        {
            Secret = DbSecret,
            AutomaticallyAfter = Duration.Days(30),
            RotateImmediatelyOnUpdate = false,
            HostedRotation = HostedRotation.PostgreSqlSingleUser(new SingleUserHostedRotationOptions
            {
                Vpc = net.Vpc,
                VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
                SecurityGroups = [net.RdsSg]
            })
        });

        // ── RDS PostgreSQL ────────────────────────────────────────────────────
        var dbInstanceType = props.DeployEnv switch
        {
            "production" => InstanceType.Of(InstanceClass.T4G, InstanceSize.MEDIUM),
            "uat"        => InstanceType.Of(InstanceClass.T4G, InstanceSize.SMALL),
            _            => InstanceType.Of(InstanceClass.T4G, InstanceSize.MICRO)
        };

        var dbSubnetGroup = new SubnetGroup(this, "DbSubnetGroup", new SubnetGroupProps
        {
            Description = "FSBS RDS isolated subnets",
            Vpc = net.Vpc,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED }
        });

        _ = new DatabaseInstance(this, "Postgres", new DatabaseInstanceProps
        {
            Engine = DatabaseInstanceEngine.Postgres(new PostgresInstanceEngineProps
            {
                Version = PostgresEngineVersion.VER_16
            }),
            InstanceType = dbInstanceType,
            Vpc = net.Vpc,
            SubnetGroup = dbSubnetGroup,
            SecurityGroups = [net.RdsSg],
            MultiAz = isProd,
            AllocatedStorage = 100,
            StorageType = StorageType.GP3,
            BackupRetention = Duration.Days(7),
            DeletionProtection = isProd,
            Credentials = Credentials.FromSecret(DbSecret),
            DatabaseName = "fsbs",
            StorageEncrypted = true,
            RemovalPolicy = isProd ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY
        });

        // ── ElastiCache Redis ─────────────────────────────────────────────────
        var redisSubnetGroup = new CfnSubnetGroup(this, "RedisSubnetGroup", new CfnSubnetGroupProps
        {
            Description = "FSBS Redis private subnets",
            SubnetIds = net.Vpc.SelectSubnets(new SubnetSelection
            {
                SubnetType = SubnetType.PRIVATE_WITH_EGRESS
            }).SubnetIds
        });

        RedisCluster = new CfnReplicationGroup(this, "Redis", new CfnReplicationGroupProps
        {
            ReplicationGroupDescription = "FSBS SignalR backplane + availability cache",
            CacheNodeType = "cache.t4g.small",
            Engine = "redis",
            NumCacheClusters = 1,
            AtRestEncryptionEnabled = true,
            TransitEncryptionEnabled = true,
            SecurityGroupIds = [net.RedisSg.SecurityGroupId],
            CacheSubnetGroupName = redisSubnetGroup.Ref,
            AutomaticFailoverEnabled = false
        });

        // ── S3 buckets ────────────────────────────────────────────────────────
        StaticBucket = new Bucket(this, "StaticBucket", new BucketProps
        {
            BucketName = $"fsbs-static-{Account}",
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            Encryption = BucketEncryption.S3_MANAGED,
            EnforceSSL = true,
            RemovalPolicy = RemovalPolicy.RETAIN,
            AutoDeleteObjects = false
        });

        DocumentsBucket = new Bucket(this, "DocumentsBucket", new BucketProps
        {
            BucketName = $"fsbs-documents-{Account}",
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            Encryption = BucketEncryption.S3_MANAGED,
            EnforceSSL = true,
            RemovalPolicy = RemovalPolicy.RETAIN,
            AutoDeleteObjects = false
        });

        // ── Additional Secrets Manager entries ────────────────────────────────
        _ = new Secret(this, "ApiKeysSecret", new SecretProps
        {
            SecretName = "fsbs/api/keys",
            Description = "FSBS API keys (SES, SNS, etc.)"
        });
    }
}
