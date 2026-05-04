using Amazon.CDK;
using Amazon.CDK.AWS.ApplicationAutoScaling;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SQS;
using Amazon.CDK.AWS.WAFv2;
using Constructs;
using FSBS.Cdk.Lambdas;
using AlbProps = Amazon.CDK.AWS.ElasticLoadBalancingV2.ApplicationLoadBalancerProps;
using Alb = Amazon.CDK.AWS.ElasticLoadBalancingV2.ApplicationLoadBalancer;
using AlbTargetGroup = Amazon.CDK.AWS.ElasticLoadBalancingV2.ApplicationTargetGroup;
using AlbTargetGroupProps = Amazon.CDK.AWS.ElasticLoadBalancingV2.ApplicationTargetGroupProps;
using AlbProtocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.ApplicationProtocol;
using AlbHealthCheck = Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck;
using AlbListenerProps = Amazon.CDK.AWS.ElasticLoadBalancingV2.BaseApplicationListenerProps;
using AlbListenerAction = Amazon.CDK.AWS.ElasticLoadBalancingV2.ListenerAction;
using AlbListenerCert = Amazon.CDK.AWS.ElasticLoadBalancingV2.ListenerCertificate;
using AlbRedirectOptions = Amazon.CDK.AWS.ElasticLoadBalancingV2.RedirectOptions;
using CfDistribution = Amazon.CDK.AWS.CloudFront.Distribution;
using CfDistributionProps = Amazon.CDK.AWS.CloudFront.DistributionProps;

namespace FSBS.Cdk.Stacks;

public class AppStackProps : StackProps
{
    public required NetworkStack Network { get; init; }
    public required DataStack Data { get; init; }
    public required string DeployEnv { get; init; }
}

public class AppStack : Stack
{
    public AppStack(Construct scope, string id, AppStackProps props) : base(scope, id, props)
    {
        var net = props.Network;
        var data = props.Data;
        var isProd = props.DeployEnv == "production";

        // ── ACM wildcard certificate ──────────────────────────────────────────
        var cert = new Certificate(this, "WildcardCert", new CertificateProps
        {
            DomainName = "*.fsbs.example.com",
            Validation = CertificateValidation.FromDns()
        });

        // ── SQS queues ────────────────────────────────────────────────────────
        var dlq = new Queue(this, "NotificationsDlq", new QueueProps
        {
            QueueName = "fsbs-notifications-dlq",
            RetentionPeriod = Duration.Days(14),
            Encryption = QueueEncryption.SQS_MANAGED
        });

        var notificationsQueue = new Queue(this, "NotificationsQueue", new QueueProps
        {
            QueueName = "fsbs-notifications",
            VisibilityTimeout = Duration.Seconds(60),
            Encryption = QueueEncryption.SQS_MANAGED,
            DeadLetterQueue = new DeadLetterQueue { Queue = dlq, MaxReceiveCount = 3 }
        });

        // ── SNS topic ─────────────────────────────────────────────────────────
        var notificationsTopic = new Topic(this, "NotificationsTopic", new TopicProps
        {
            TopicName = "fsbs-notifications",
            DisplayName = "FSBS Notifications"
        });
        notificationsTopic.AddSubscription(
            new Amazon.CDK.AWS.SNS.Subscriptions.SqsSubscription(notificationsQueue));

        // ── Cognito Lambda triggers ───────────────────────────────────────────
        var preSignUp = new PreSignUpFunction(this, "PreSignUpFn");
        var postConfirmation = new PostConfirmationFunction(this, "PostConfirmationFn");
        var tokenRefresh = new TokenRefreshFunction(this, "TokenRefreshFn");

        // ── Cognito Staff Pool (Entra ID OIDC federation) ─────────────────────
        var staffPool = new UserPool(this, "StaffPool", new UserPoolProps
        {
            UserPoolName = "fsbs-staff-pool",
            SelfSignUpEnabled = false,
            SignInAliases = new SignInAliases { Email = true },
            AutoVerify = new AutoVerifiedAttrs { Email = true },
            StandardAttributes = new StandardAttributes
            {
                Email = new StandardAttribute { Required = true, Mutable = true }
            },
            CustomAttributes = new Dictionary<string, ICustomAttribute>
            {
                ["entra_groups"] = new StringAttribute(new StringAttributeProps { Mutable = true })
            },
            LambdaTriggers = new UserPoolTriggers
            {
                PostConfirmation = postConfirmation,
                PreTokenGeneration = tokenRefresh
            },
            PasswordPolicy = new PasswordPolicy
            {
                MinLength = 12,
                RequireUppercase = true,
                RequireLowercase = true,
                RequireDigits = true,
                RequireSymbols = true
            },
            AccountRecovery = AccountRecovery.NONE,
            RemovalPolicy = RemovalPolicy.RETAIN
        });

        // Entra ID OIDC identity provider
        var entraIdp = new UserPoolIdentityProviderOidc(this, "EntraIdp", new UserPoolIdentityProviderOidcProps
        {
            UserPool = staffPool,
            Name = "EntraID",
            ClientId = "ENTRA_CLIENT_ID_PLACEHOLDER",
            ClientSecret = "ENTRA_CLIENT_SECRET_PLACEHOLDER",
            IssuerUrl = "https://login.microsoftonline.com/TENANT_ID/v2.0",
            Scopes = ["openid", "email", "profile"],
            AttributeMapping = new AttributeMapping
            {
                Email = ProviderAttribute.Other("email"),
                Custom = new Dictionary<string, ProviderAttribute>
                {
                    ["custom:entra_groups"] = ProviderAttribute.Other("groups")
                }
            }
        });

        var staffPoolClient = staffPool.AddClient("StaffPoolClient", new UserPoolClientOptions
        {
            UserPoolClientName = "fsbs-staff-client",
            SupportedIdentityProviders = [UserPoolClientIdentityProvider.Custom("EntraID")],
            OAuth = new OAuthSettings
            {
                Flows = new OAuthFlows { AuthorizationCodeGrant = true },
                Scopes = [OAuthScope.OPENID, OAuthScope.EMAIL, OAuthScope.PROFILE],
                CallbackUrls = ["https://app.fsbs.example.com/auth/callback/staff"],
                LogoutUrls = ["https://app.fsbs.example.com/logout"]
            },
            GenerateSecret = true
        });
        staffPoolClient.Node.AddDependency(entraIdp);

        // Staff Cognito groups
        foreach (var role in new[] { "SystemAdmin", "ScheduleAdmin", "CourseDirector", "Instructor", "Management", "SalesStaff", "InternalStudent" })
        {
            _ = new CfnUserPoolGroup(this, $"StaffGroup{role}", new CfnUserPoolGroupProps
            {
                UserPoolId = staffPool.UserPoolId,
                GroupName = role,
                Description = $"FSBS staff role: {role}"
            });
        }

        // ── Cognito Customer Pool (invitation-only) ───────────────────────────
        var customerPool = new UserPool(this, "CustomerPool", new UserPoolProps
        {
            UserPoolName = "fsbs-customer-pool",
            SelfSignUpEnabled = false,
            SignInAliases = new SignInAliases { Email = true },
            AutoVerify = new AutoVerifiedAttrs { Email = true },
            StandardAttributes = new StandardAttributes
            {
                Email = new StandardAttribute { Required = true, Mutable = true },
                GivenName = new StandardAttribute { Required = true, Mutable = true },
                FamilyName = new StandardAttribute { Required = true, Mutable = true }
            },
            CustomAttributes = new Dictionary<string, ICustomAttribute>
            {
                ["org_id"]   = new StringAttribute(new StringAttributeProps { Mutable = false }),
                ["app_role"] = new StringAttribute(new StringAttributeProps { Mutable = true })
            },
            LambdaTriggers = new UserPoolTriggers
            {
                PreSignUp = preSignUp,
                PostConfirmation = postConfirmation
            },
            PasswordPolicy = new PasswordPolicy
            {
                MinLength = 12,
                RequireUppercase = true,
                RequireLowercase = true,
                RequireDigits = true,
                RequireSymbols = true
            },
            AccountRecovery = AccountRecovery.EMAIL_ONLY,
            RemovalPolicy = RemovalPolicy.RETAIN
        });

        _ = customerPool.AddClient("CustomerPoolClient", new UserPoolClientOptions
        {
            UserPoolClientName = "fsbs-customer-client",
            SupportedIdentityProviders = [UserPoolClientIdentityProvider.COGNITO],
            OAuth = new OAuthSettings
            {
                Flows = new OAuthFlows { AuthorizationCodeGrant = true },
                Scopes = [OAuthScope.OPENID, OAuthScope.EMAIL, OAuthScope.PROFILE],
                CallbackUrls = ["https://app.fsbs.example.com/auth/callback/customer"],
                LogoutUrls = ["https://app.fsbs.example.com/logout"]
            },
            GenerateSecret = false
        });

        // ── ECS Cluster ───────────────────────────────────────────────────────
        var cluster = new Cluster(this, "Cluster", new ClusterProps
        {
            Vpc = net.Vpc,
            ClusterName = "fsbs"
        });

        // ── IAM task role ─────────────────────────────────────────────────────
        var taskRole = new Role(this, "ApiTaskRole", new RoleProps
        {
            AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
            Description = "FSBS API Fargate task role"
        });
        data.DbSecret.GrantRead(taskRole);
        data.StaticBucket.GrantReadWrite(taskRole);
        data.DocumentsBucket.GrantReadWrite(taskRole);
        notificationsQueue.GrantSendMessages(taskRole);
        notificationsTopic.GrantPublish(taskRole);
        taskRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AWSXRayDaemonWriteAccess"));

        // ── ALB ───────────────────────────────────────────────────────────────
        var alb = new Alb(this, "Alb", new AlbProps
        {
            Vpc = net.Vpc,
            InternetFacing = true,
            SecurityGroup = net.AlbSg,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
            LoadBalancerName = "fsbs-alb"
        });

        var apiLogGroup = new LogGroup(this, "ApiLogGroup", new LogGroupProps
        {
            LogGroupName = "/fsbs/api",
            Retention = RetentionDays.ONE_YEAR,
            RemovalPolicy = RemovalPolicy.RETAIN
        });

        // ── API Fargate service ───────────────────────────────────────────────
        var apiTaskDef = new FargateTaskDefinition(this, "ApiTaskDef", new FargateTaskDefinitionProps
        {
            Cpu = 1024,
            MemoryLimitMiB = 2048,
            TaskRole = taskRole
        });

        apiTaskDef.AddContainer("Api", new ContainerDefinitionOptions
        {
            Image = ContainerImage.FromRegistry("amazon/amazon-ecs-sample"),
            PortMappings = [new PortMapping { ContainerPort = 8080 }],
            Logging = LogDriver.AwsLogs(new AwsLogDriverProps
            {
                LogGroup = apiLogGroup,
                StreamPrefix = "api"
            }),
            Environment = new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = isProd ? "Production" : "Staging",
                ["AWS_REGION"] = Region
            },
            Secrets = new Dictionary<string, Amazon.CDK.AWS.ECS.Secret>
            {
                ["ConnectionStrings__Default"] = Amazon.CDK.AWS.ECS.Secret.FromSecretsManager(data.DbSecret)
            }
        });

        var apiService = new FargateService(this, "ApiService", new FargateServiceProps
        {
            Cluster = cluster,
            TaskDefinition = apiTaskDef,
            DesiredCount = 2,
            SecurityGroups = [net.ApiSg],
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
            AssignPublicIp = false,
            ServiceName = "fsbs-api",
            CircuitBreaker = new DeploymentCircuitBreaker { Rollback = true }
        });

        // Auto-scaling: min 2, max 10, CPU 60%
        var scaling = apiService.AutoScaleTaskCount(new EnableScalingProps { MinCapacity = 2, MaxCapacity = 10 });
        scaling.ScaleOnCpuUtilization("CpuScaling", new CpuUtilizationScalingProps
        {
            TargetUtilizationPercent = 60
        });

        // ALB target group + HTTPS listener
        var targetGroup = new AlbTargetGroup(this, "ApiTargetGroup", new AlbTargetGroupProps
        {
            Vpc = net.Vpc,
            Port = 8080,
            Protocol = AlbProtocol.HTTP,
            Targets = [apiService],
            HealthCheck = new AlbHealthCheck
            {
                Path = "/health",
                HealthyHttpCodes = "200",
                Interval = Duration.Seconds(30),
                Timeout = Duration.Seconds(5),
                HealthyThresholdCount = 2,
                UnhealthyThresholdCount = 3
            },
            DeregistrationDelay = Duration.Seconds(30)
        });

        alb.AddListener("HttpsListener", new AlbListenerProps
        {
            Port = 443,
            Protocol = AlbProtocol.HTTPS,
            Certificates = [AlbListenerCert.FromCertificateManager(cert)],
            DefaultTargetGroups = [targetGroup]
        });

        alb.AddListener("HttpRedirect", new AlbListenerProps
        {
            Port = 80,
            Protocol = AlbProtocol.HTTP,
            DefaultAction = AlbListenerAction.Redirect(new AlbRedirectOptions
            {
                Protocol = "HTTPS",
                Port = "443",
                Permanent = true
            })
        });

        // ── Worker Fargate service (SQS consumer) ─────────────────────────────
        var workerTaskDef = new FargateTaskDefinition(this, "WorkerTaskDef", new FargateTaskDefinitionProps
        {
            Cpu = 512,
            MemoryLimitMiB = 1024,
            TaskRole = taskRole
        });

        workerTaskDef.AddContainer("Worker", new ContainerDefinitionOptions
        {
            Image = ContainerImage.FromRegistry("amazon/amazon-ecs-sample"),
            Logging = LogDriver.AwsLogs(new AwsLogDriverProps
            {
                LogGroup = new LogGroup(this, "WorkerLogGroup", new LogGroupProps
                {
                    LogGroupName = "/fsbs/worker",
                    Retention = RetentionDays.ONE_YEAR,
                    RemovalPolicy = RemovalPolicy.RETAIN
                }),
                StreamPrefix = "worker"
            }),
            Environment = new Dictionary<string, string>
            {
                ["SQS_QUEUE_URL"] = notificationsQueue.QueueUrl,
                ["AWS_REGION"] = Region
            }
        });

        _ = new FargateService(this, "WorkerService", new FargateServiceProps
        {
            Cluster = cluster,
            TaskDefinition = workerTaskDef,
            DesiredCount = 1,
            SecurityGroups = [net.ApiSg],
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
            AssignPublicIp = false,
            ServiceName = "fsbs-worker"
        });

        // ── WAF WebACL (CLOUDFRONT scope — must be in us-east-1) ─────────────
        var wafAcl = new CfnWebACL(this, "WafAcl", new CfnWebACLProps
        {
            Name = "fsbs-waf",
            Scope = "CLOUDFRONT",
            DefaultAction = new CfnWebACL.DefaultActionProperty { Allow = new CfnWebACL.AllowActionProperty() },
            VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
            {
                SampledRequestsEnabled = true,
                CloudWatchMetricsEnabled = true,
                MetricName = "fsbs-waf"
            },
            Rules = new object[]
            {
                // Rate limit: 300 req / 5 min per IP
                new CfnWebACL.RuleProperty
                {
                    Name = "RateLimit",
                    Priority = 1,
                    Action = new CfnWebACL.RuleActionProperty { Block = new CfnWebACL.BlockActionProperty() },
                    VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
                    {
                        SampledRequestsEnabled = true,
                        CloudWatchMetricsEnabled = true,
                        MetricName = "RateLimit"
                    },
                    Statement = new CfnWebACL.StatementProperty
                    {
                        RateBasedStatement = new CfnWebACL.RateBasedStatementProperty
                        {
                            Limit = 300,
                            AggregateKeyType = "IP",
                            EvaluationWindowSec = 300
                        }
                    }
                },
                // OWASP Core Rule Set
                new CfnWebACL.RuleProperty
                {
                    Name = "AWSManagedRulesCommonRuleSet",
                    Priority = 2,
                    OverrideAction = new CfnWebACL.OverrideActionProperty { None = new object() },
                    VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
                    {
                        SampledRequestsEnabled = true,
                        CloudWatchMetricsEnabled = true,
                        MetricName = "AWSManagedRulesCommonRuleSet"
                    },
                    Statement = new CfnWebACL.StatementProperty
                    {
                        ManagedRuleGroupStatement = new CfnWebACL.ManagedRuleGroupStatementProperty
                        {
                            VendorName = "AWS",
                            Name = "AWSManagedRulesCommonRuleSet"
                        }
                    }
                },
                // SQL injection managed rule
                new CfnWebACL.RuleProperty
                {
                    Name = "AWSManagedRulesSQLiRuleSet",
                    Priority = 3,
                    OverrideAction = new CfnWebACL.OverrideActionProperty { None = new object() },
                    VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
                    {
                        SampledRequestsEnabled = true,
                        CloudWatchMetricsEnabled = true,
                        MetricName = "AWSManagedRulesSQLiRuleSet"
                    },
                    Statement = new CfnWebACL.StatementProperty
                    {
                        ManagedRuleGroupStatement = new CfnWebACL.ManagedRuleGroupStatementProperty
                        {
                            VendorName = "AWS",
                            Name = "AWSManagedRulesSQLiRuleSet"
                        }
                    }
                }
            }
        });

        // ── CloudFront distribution ───────────────────────────────────────────
        var oac = new S3OriginAccessControl(this, "Oac", new S3OriginAccessControlProps
        {
            Description = "FSBS static assets OAC"
        });

        var staticOrigin = S3BucketOrigin.WithOriginAccessControl(data.StaticBucket, new S3BucketOriginWithOACProps
        {
            OriginAccessControl = oac
        });

        var apiOrigin = new LoadBalancerV2Origin(alb, new LoadBalancerV2OriginProps
        {
            ProtocolPolicy = OriginProtocolPolicy.HTTPS_ONLY,
            HttpsPort = 443
        });

        var distribution = new CfDistribution(this, "Cdn", new CfDistributionProps
        {
            DefaultBehavior = new BehaviorOptions
            {
                Origin = staticOrigin,
                ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                CachePolicy = CachePolicy.CACHING_OPTIMIZED,
                AllowedMethods = AllowedMethods.ALLOW_GET_HEAD
            },
            AdditionalBehaviors = new Dictionary<string, IBehaviorOptions>
            {
                ["/v1/*"] = new BehaviorOptions
                {
                    Origin = apiOrigin,
                    ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                    CachePolicy = CachePolicy.CACHING_DISABLED,
                    AllowedMethods = AllowedMethods.ALLOW_ALL,
                    OriginRequestPolicy = OriginRequestPolicy.ALL_VIEWER_EXCEPT_HOST_HEADER
                },
                ["/hubs/*"] = new BehaviorOptions
                {
                    Origin = apiOrigin,
                    ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                    CachePolicy = CachePolicy.CACHING_DISABLED,
                    AllowedMethods = AllowedMethods.ALLOW_ALL,
                    OriginRequestPolicy = OriginRequestPolicy.ALL_VIEWER_EXCEPT_HOST_HEADER
                }
            },
            Certificate = cert,
            DomainNames = ["app.fsbs.example.com"],
            PriceClass = PriceClass.PRICE_CLASS_100,
            WebAclId = wafAcl.AttrArn,
            DefaultRootObject = "index.html",
            ErrorResponses =
            [
                new ErrorResponse { HttpStatus = 403, ResponseHttpStatus = 200, ResponsePagePath = "/index.html", Ttl = Duration.Seconds(0) },
                new ErrorResponse { HttpStatus = 404, ResponseHttpStatus = 200, ResponsePagePath = "/index.html", Ttl = Duration.Seconds(0) }
            ]
        });

        // ── CloudWatch alarms ─────────────────────────────────────────────────
        _ = new Alarm(this, "ApiP95LatencyAlarm", new AlarmProps
        {
            AlarmName = "fsbs-api-p95-latency",
            AlarmDescription = "API p95 latency > 400ms",
            Metric = new Metric(new MetricProps
            {
                Namespace = "AWS/ApplicationELB",
                MetricName = "TargetResponseTime",
                DimensionsMap = new Dictionary<string, string>
                {
                    ["LoadBalancer"] = alb.LoadBalancerFullName
                },
                Statistic = "p95",
                Period = Duration.Minutes(1)
            }),
            Threshold = 0.4,
            EvaluationPeriods = 3,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD,
            TreatMissingData = TreatMissingData.NOT_BREACHING
        });

        _ = new Alarm(this, "DlqDepthAlarm", new AlarmProps
        {
            AlarmName = "fsbs-notifications-dlq-depth",
            AlarmDescription = "Notifications DLQ has messages — worker failures",
            Metric = dlq.MetricApproximateNumberOfMessagesVisible(),
            Threshold = 1,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
            TreatMissingData = TreatMissingData.NOT_BREACHING
        });

        // ── Outputs ───────────────────────────────────────────────────────────
        _ = new CfnOutput(this, "CdnDomain", new CfnOutputProps
        {
            Value = distribution.DomainName,
            Description = "CloudFront distribution domain"
        });
        _ = new CfnOutput(this, "StaffPoolId", new CfnOutputProps
        {
            Value = staffPool.UserPoolId,
            Description = "Cognito Staff Pool ID"
        });
        _ = new CfnOutput(this, "CustomerPoolId", new CfnOutputProps
        {
            Value = customerPool.UserPoolId,
            Description = "Cognito Customer Pool ID"
        });
        _ = new CfnOutput(this, "NotificationsQueueUrl", new CfnOutputProps
        {
            Value = notificationsQueue.QueueUrl,
            Description = "SQS notifications queue URL"
        });
    }
}
