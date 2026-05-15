using Amazon.CDK;
using FSBS.Cdk.Stacks;

var app = new App();

var env = new Amazon.CDK.Environment
{
    Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
    Region  = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
};

var usEast1Env = new Amazon.CDK.Environment
{
    Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
    Region  = "us-east-1"
};

// Derive deploy environment from CDK context or default to "staging"
var deployEnv = app.Node.TryGetContext("deployEnv") as string ?? "staging";
var rootDomain = app.Node.TryGetContext("rootDomain") as string ?? "fsbs.tqaentry.com";
var cloudFrontPrefixListId = app.Node.TryGetContext("cloudFrontPrefixListId") as string;
var apiImageUri = app.Node.TryGetContext("apiImageUri") as string;
var workerImageUri = app.Node.TryGetContext("workerImageUri") as string;
var entraClientId = app.Node.TryGetContext("entraClientId") as string;
var entraTenantId = app.Node.TryGetContext("entraTenantId") as string;
var entraClientSecret = app.Node.TryGetContext("entraClientSecret") as string;
var rootTenantId = app.Node.TryGetContext("rootTenantId") as string;

// During cdk bootstrap the app is not synthesised — skip stack construction.
if (args.Contains("bootstrap"))
{
    app.Synth();
    return;
}

if (apiImageUri is null) throw new InvalidOperationException("CDK context 'apiImageUri' is required.");
if (workerImageUri is null) throw new InvalidOperationException("CDK context 'workerImageUri' is required.");
if (entraClientId is null) throw new InvalidOperationException("CDK context 'entraClientId' is required.");
if (entraTenantId is null) throw new InvalidOperationException("CDK context 'entraTenantId' is required.");
if (entraClientSecret is null) throw new InvalidOperationException("CDK context 'entraClientSecret' is required.");
if (rootTenantId is null) throw new InvalidOperationException("CDK context 'rootTenantId' is required (school's root tenant_id GUID).");

var network = new NetworkStack(app, "FsbsNetworkStack", new NetworkStackProps
{
    Env = env,
    CloudFrontPrefixListId = cloudFrontPrefixListId
});

var data = new DataStack(app, "FsbsDataStack", new DataStackProps
{
    Env = env,
    Network = network,
    DeployEnv = deployEnv
});
data.AddDependency(network);

// WAF WebACL and ACM certificate must be in us-east-1 for CloudFront.
// CrossRegionReferences must be enabled on both stacks for the ARN reference to resolve.
var waf = new WafStack(app, "FsbsWafStack", new StackProps
{
    Env = usEast1Env,
    CrossRegionReferences = true
});

var cert = new CertStack(app, "FsbsCertStack", new CertStackProps
{
    Env = usEast1Env,
    CrossRegionReferences = true,
    RootDomain = rootDomain
});

var appStack = new AppStack(app, "FsbsAppStack", new AppStackProps
{
    Env = env,
    CrossRegionReferences = true,
    Network = network,
    Data = data,
    DeployEnv = deployEnv,
    RootDomain = rootDomain,
    ApiImageUri = apiImageUri,
    WorkerImageUri = workerImageUri,
    RootTenantId = rootTenantId,
    EntraClientId = entraClientId,
    EntraTenantId = entraTenantId,
    EntraClientSecret = entraClientSecret,
    WebAclArn = waf.WebAclArn,
    Certificate = cert.Certificate
});
appStack.AddDependency(waf);
appStack.AddDependency(cert);

app.Synth();
