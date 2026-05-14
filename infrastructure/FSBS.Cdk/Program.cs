using Amazon.CDK;
using FSBS.Cdk.Stacks;

var app = new App();

var env = new Amazon.CDK.Environment
{
    Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
    Region  = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
};

// Derive deploy environment from CDK context or default to "staging"
var deployEnv = app.Node.TryGetContext("deployEnv") as string ?? "staging";
var rootDomain = app.Node.TryGetContext("rootDomain") as string ?? "fsbs.tqaentry.com";
var cloudFrontPrefixListId = app.Node.TryGetContext("cloudFrontPrefixListId") as string;
var apiImageUri = app.Node.TryGetContext("apiImageUri") as string
    ?? throw new InvalidOperationException("CDK context 'apiImageUri' is required.");
var workerImageUri = app.Node.TryGetContext("workerImageUri") as string
    ?? throw new InvalidOperationException("CDK context 'workerImageUri' is required.");
var entraClientId = app.Node.TryGetContext("entraClientId") as string
    ?? throw new InvalidOperationException("CDK context 'entraClientId' is required.");
var entraTenantId = app.Node.TryGetContext("entraTenantId") as string
    ?? throw new InvalidOperationException("CDK context 'entraTenantId' is required.");
var rootTenantId = app.Node.TryGetContext("rootTenantId") as string
    ?? throw new InvalidOperationException(
        "CDK context 'rootTenantId' is required (school's root tenant_id GUID).");

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

var appStack = new AppStack(app, "FsbsAppStack", new AppStackProps
{
    Env = env,
    Network = network,
    Data = data,
    DeployEnv = deployEnv,
    RootDomain = rootDomain,
    ApiImageUri = apiImageUri,
    WorkerImageUri = workerImageUri,
    RootTenantId = rootTenantId,
    EntraClientId = entraClientId,
    EntraTenantId = entraTenantId
});

app.Synth();
