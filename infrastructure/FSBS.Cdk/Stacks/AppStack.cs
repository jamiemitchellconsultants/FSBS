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
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SQS;
using Amazon.CDK.CustomResources;
using Constructs;
using FSBS.Cdk.Lambdas;
using AlbProps = Amazon.CDK.AWS.ElasticLoadBalancingV2.ApplicationLoadBalancerProps;
using Alb = Amazon.CDK.AWS.ElasticLoadBalancingV2.ApplicationLoadBalancer;
using AlbTargetGroup = Amazon.CDK.AWS.ElasticLoadBalancingV2.ApplicationTargetGroup;
using AlbTargetGroupProps = Amazon.CDK.AWS.ElasticLoadBalancingV2.ApplicationTargetGroupProps;
using AlbProtocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.ApplicationProtocol;
using AlbHealthCheck = Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck;
using AlbListenerProps = Amazon.CDK.AWS.ElasticLoadBalancingV2.BaseApplicationListenerProps;
using AlbListenerCert = Amazon.CDK.AWS.ElasticLoadBalancingV2.ListenerCertificate;
using CfDistribution = Amazon.CDK.AWS.CloudFront.Distribution;
using CfDistributionProps = Amazon.CDK.AWS.CloudFront.DistributionProps;
using LambdaFunction = Amazon.CDK.AWS.Lambda.Function;
using SecretsManagerSecret = Amazon.CDK.AWS.SecretsManager.Secret;

namespace FSBS.Cdk.Stacks;

public class AppStackProps : StackProps
{
    public required NetworkStack Network { get; init; }
    public required DataStack Data { get; init; }
    public required string DeployEnv { get; init; }
    public required string ApiImageUri { get; init; }
    public required string WorkerImageUri { get; init; }
    public required string EntraClientId { get; init; }
    public required string EntraTenantId { get; init; }
    public required string EntraClientSecret { get; init; }

    /// <summary>
    /// Root domain for this deployment, e.g. <c>fsbs.tqaentry.com</c>.
    /// App subdomain is derived from <see cref="DeployEnv"/>.
    /// </summary>
    public required string RootDomain { get; init; }

    /// <summary>
    /// ARN of the WAFv2 WebACL from <see cref="WafStack"/> (must be in us-east-1).
    /// </summary>
    public required string WebAclArn { get; init; }

    /// <summary>
    /// School-wide root tenant_id used for staff and private customer
    /// provisioning. Stored as a Cognito Lambda env var.
    /// </summary>
    public string? RootTenantId { get; init; }
}

public class AppStack : Stack
{
    public AppStack(Construct scope, string id, AppStackProps props) : base(scope, id, props)
    {
        var net = props.Network;
        var data = props.Data;
        var isProd = props.DeployEnv == "production";

        var appDomain = props.DeployEnv switch
        {
            "production" => $"app.{props.RootDomain}",
            "uat"        => $"uat.{props.RootDomain}",
            _            => $"staging.{props.RootDomain}"
        };

        // Import buckets by deterministic names to avoid cross-stack bucket
        // policy mutations that can create dependency cycles with CloudFront.
        var staticBucket = Bucket.FromBucketName(this, "StaticBucketImported", $"fsbs-static-{Account}");
        var documentsBucket = Bucket.FromBucketName(this, "DocumentsBucketImported", $"fsbs-documents-{Account}");

        // ── ACM wildcard certificate ──────────────────────────────────────────
        var cert = new Certificate(this, "WildcardCert", new CertificateProps
        {
            DomainName = $"*.{props.RootDomain}",
            Validation = CertificateValidation.FromDns()
        });

        // ── SQS queues ────────────────────────────────────────────────────────
        var bookingEventsDlq = new Queue(this, "BookingEventsDlq", new QueueProps
        {
            QueueName = "fsbs-booking-events-dlq",
            RetentionPeriod = Duration.Days(14),
            Encryption = QueueEncryption.SQS_MANAGED
        });

        var bookingEventsQueue = new Queue(this, "BookingEventsQueue", new QueueProps
        {
            QueueName = "fsbs-booking-events",
            VisibilityTimeout = Duration.Seconds(60),
            Encryption = QueueEncryption.SQS_MANAGED,
            DeadLetterQueue = new DeadLetterQueue { Queue = bookingEventsDlq, MaxReceiveCount = 3 }
        });

        // ── SNS topic ─────────────────────────────────────────────────────────
        var bookingEventsTopic = new Topic(this, "BookingEventsTopic", new TopicProps
        {
            TopicName = "fsbs-booking-events",
            DisplayName = "FSBS Booking Events"
        });
        bookingEventsTopic.AddSubscription(
            new Amazon.CDK.AWS.SNS.Subscriptions.SqsSubscription(bookingEventsQueue));

        // ── Cognito Lambda triggers ───────────────────────────────────────────
        var preSignUp        = new PreSignUpFunction(this, "PreSignUpFn", net.Vpc, net.LambdaSg);
        var postConfirmation = new PostConfirmationFunction(this, "PostConfirmationFn", net.Vpc, net.LambdaSg);
        var tokenRefresh     = new TokenRefreshFunction(this, "TokenRefreshFn", net.Vpc, net.LambdaSg);

        var dbEndpointEnv = new Dictionary<string, string>
        {
            ["FSBS_DB_SECRET_ARN"] = data.AppDbSecret.SecretArn,
            ["FSBS_DB_HOST"]       = data.Postgres.DbInstanceEndpointAddress,
            ["FSBS_DB_PORT"]       = data.Postgres.DbInstanceEndpointPort,
            ["FSBS_DB_NAME"]       = "fsbs"
        };

        foreach (var fn in new LambdaFunction[] { preSignUp, postConfirmation, tokenRefresh })
        {
            foreach (var kv in dbEndpointEnv) fn.AddEnvironment(kv.Key, kv.Value);
            data.AppDbSecret.GrantRead(fn);
        }

        // PostConfirmation also needs the school's root tenant id (used when
        // provisioning staff and private customer rows).
        var rootTenantId = props.RootTenantId
            ?? throw new InvalidOperationException(
                "AppStackProps.RootTenantId must be set (school's root tenant_id GUID).");
        postConfirmation.AddEnvironment("FSBS_ROOT_TENANT_ID", rootTenantId);

        // PostConfirmation + TokenRefresh administer Cognito group membership.
        var cognitoAdminPolicy = new PolicyStatement(new PolicyStatementProps
        {
            Effect    = Effect.ALLOW,
            Actions   = ["cognito-idp:AdminAddUserToGroup",
                         "cognito-idp:AdminRemoveUserFromGroup",
                         "cognito-idp:AdminListGroupsForUser"],
            Resources = ["*"]   // Refined to specific UserPool ARNs after they exist (below).
        });
        postConfirmation.AddToRolePolicy(cognitoAdminPolicy);
        tokenRefresh.AddToRolePolicy(cognitoAdminPolicy);

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
            ClientId = props.EntraClientId,
            ClientSecret = props.EntraClientSecret,
            IssuerUrl = $"https://login.microsoftonline.com/{props.EntraTenantId}/v2.0",
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
                CallbackUrls = [$"https://{appDomain}/auth/callback/staff"],
                LogoutUrls = [$"https://{appDomain}/logout"]
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
                CallbackUrls = [$"https://{appDomain}/auth/callback/customer"],
                LogoutUrls = [$"https://{appDomain}/logout"]
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

        // Execution roles are created explicitly so GrantRead calls have a
        // non-null grantee at construction time. The auto-created ExecutionRole
        // on FargateTaskDefinition is null until synthesis and causes a jsii
        // SerializationError when passed to Secret.GrantRead.
        var apiExecutionRole = new Role(this, "ApiExecutionRole", new RoleProps
        {
            AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
            ManagedPolicies = [ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonECSTaskExecutionRolePolicy")]
        });
        data.AppDbSecret.GrantRead(apiExecutionRole);
        data.ApiKeysSecret.GrantRead(apiExecutionRole);

        var workerExecutionRole = new Role(this, "WorkerExecutionRole", new RoleProps
        {
            AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
            ManagedPolicies = [ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonECSTaskExecutionRolePolicy")]
        });
        data.AppDbSecret.GrantRead(workerExecutionRole);
        data.ApiKeysSecret.GrantRead(workerExecutionRole);

        // ECS tasks read the runtime fsbs_app credentials, never the master.
        data.AppDbSecret.GrantRead(taskRole);
        data.ApiKeysSecret.GrantRead(taskRole);
        staticBucket.GrantReadWrite(taskRole);
        documentsBucket.GrantReadWrite(taskRole);
        bookingEventsQueue.GrantSendMessages(taskRole);
        bookingEventsQueue.GrantConsumeMessages(taskRole);
        bookingEventsTopic.GrantPublish(taskRole);
        taskRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions =
            [
                "ses:SendEmail",
                "ses:SendTemplatedEmail",
                "ses:CreateTemplate",
                "ses:UpdateTemplate"
            ],
            Resources = ["*"]
        }));
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
            RemovalPolicy = isProd ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY
        });

        // ── API Fargate service ───────────────────────────────────────────────
        var apiTaskDef = new FargateTaskDefinition(this, "ApiTaskDef", new FargateTaskDefinitionProps
        {
            Cpu = 1024,
            MemoryLimitMiB = 2048,
            TaskRole = taskRole,
            ExecutionRole = apiExecutionRole
        });

        apiTaskDef.AddContainer("Api", new ContainerDefinitionOptions
        {
            Image = ContainerImage.FromRegistry(props.ApiImageUri),
            PortMappings = [new PortMapping { ContainerPort = 8080 }],
            Logging = LogDriver.AwsLogs(new AwsLogDriverProps
            {
                LogGroup = apiLogGroup,
                StreamPrefix = "api"
            }),
            Environment = new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = isProd ? "Production" : "Staging",
                ["AWS_REGION"] = Region,
                ["Sqs__BookingEventsQueueUrl"] = bookingEventsQueue.QueueUrl
            },
            Secrets = new Dictionary<string, Amazon.CDK.AWS.ECS.Secret>
            {
                ["ConnectionStrings__Default"] = Amazon.CDK.AWS.ECS.Secret.FromSecretsManager(data.AppDbSecret),
                ["Secrets__DbCredentials"] = Amazon.CDK.AWS.ECS.Secret.FromSecretsManager(data.AppDbSecret),
                ["Secrets__ApiKeys"] = Amazon.CDK.AWS.ECS.Secret.FromSecretsManager(data.ApiKeysSecret)
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

        // ── Worker Fargate service (SQS consumer) ─────────────────────────────
        var workerTaskDef = new FargateTaskDefinition(this, "WorkerTaskDef", new FargateTaskDefinitionProps
        {
            Cpu = 512,
            MemoryLimitMiB = 1024,
            TaskRole = taskRole,
            ExecutionRole = workerExecutionRole
        });

        workerTaskDef.AddContainer("Worker", new ContainerDefinitionOptions
        {
            Image = ContainerImage.FromRegistry(props.WorkerImageUri),
            Logging = LogDriver.AwsLogs(new AwsLogDriverProps
            {
                LogGroup = new LogGroup(this, "WorkerLogGroup", new LogGroupProps
                {
                    LogGroupName = "/fsbs/worker",
                    Retention = RetentionDays.ONE_YEAR,
                    RemovalPolicy = isProd ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY
                }),
                StreamPrefix = "worker"
            }),
            Environment = new Dictionary<string, string>
            {
                ["Worker__BookingEventsQueueUrl"] = bookingEventsQueue.QueueUrl,
                ["AWS_REGION"] = Region
            },
            Secrets = new Dictionary<string, Amazon.CDK.AWS.ECS.Secret>
            {
                ["Database__ConnectionString"] = Amazon.CDK.AWS.ECS.Secret.FromSecretsManager(data.AppDbSecret),
                ["Secrets__DbCredentials"] = Amazon.CDK.AWS.ECS.Secret.FromSecretsManager(data.AppDbSecret),
                ["Secrets__ApiKeys"] = Amazon.CDK.AWS.ECS.Secret.FromSecretsManager(data.ApiKeysSecret)
            }
        });

        var workerService = new FargateService(this, "WorkerService", new FargateServiceProps
        {
            Cluster = cluster,
            TaskDefinition = workerTaskDef,
            DesiredCount = 1,
            SecurityGroups = [net.ApiSg],
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
            AssignPublicIp = false,
            ServiceName = "fsbs-worker"
        });

        // ── DB grants Custom Resource ─────────────────────────────────────────
        // Provisions the runtime fsbs_app + fsbs_readonly roles inside the RDS
        // instance using the master credentials. Idempotent — safe to re-run.
        // Skip on first deploy (pass -c skipDbGrants=true) until the schema
        // has been applied via fsbs_schema.sql.
        var skipDbGrants = Node.TryGetContext("skipDbGrants") as string == "true";

        CustomResource? dbGrants = null;
        if (!skipDbGrants)
        {
            var dbGrantsFn = new DbGrantsFunction(this, "DbGrantsFn", net.Vpc, net.LambdaSg);
            data.DbSecret.GrantRead(dbGrantsFn);
            data.AppDbSecret.GrantRead(dbGrantsFn);
            data.ReadonlyDbSecret.GrantRead(dbGrantsFn);

            var dbGrantsProvider = new Provider(this, "DbGrantsProvider", new ProviderProps
            {
                OnEventHandler = dbGrantsFn,
                LogRetention   = RetentionDays.ONE_MONTH
            });

            dbGrants = new CustomResource(this, "DbGrants", new CustomResourceProps
            {
                ServiceToken = dbGrantsProvider.ServiceToken,
                Properties   = new Dictionary<string, object>
                {
                    ["DbHost"]            = data.Postgres.DbInstanceEndpointAddress,
                    ["DbPort"]            = data.Postgres.DbInstanceEndpointPort,
                    ["DbName"]            = "fsbs",
                    ["MasterSecretArn"]   = data.DbSecret.SecretArn,
                    ["AppSecretArn"]      = data.AppDbSecret.SecretArn,
                    ["ReadonlySecretArn"] = data.ReadonlyDbSecret.SecretArn,
                    ["RotationToken"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
                }
            });
            dbGrants.Node.AddDependency(data.Postgres);
        }

        // ECS tasks must not start until fsbs_app role exists in the DB.
        if (dbGrants is not null)
        {
            apiService.Node.AddDependency(dbGrants);
            workerService.Node.AddDependency(dbGrants);
        }

        // ── CloudFront distribution ───────────────────────────────────────────
        var oac = new S3OriginAccessControl(this, "Oac", new S3OriginAccessControlProps
        {
            Description = "FSBS static assets OAC"
        });

        var staticOrigin = S3BucketOrigin.WithOriginAccessControl(staticBucket, new S3BucketOriginWithOACProps
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
            DomainNames = [appDomain],
            PriceClass = PriceClass.PRICE_CLASS_100,
            WebAclId = props.WebAclArn,
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
            AlarmName = "fsbs-booking-events-dlq-depth",
            AlarmDescription = "Booking events DLQ has messages — worker failures",
            Metric = bookingEventsDlq.MetricApproximateNumberOfMessagesVisible(),
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
        _ = new CfnOutput(this, "AppUrl", new CfnOutputProps
        {
            Value = $"https://{appDomain}",
            Description = "Application URL"
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
            Value = bookingEventsQueue.QueueUrl,
            Description = "SQS booking events queue URL"
        });
    }
}
