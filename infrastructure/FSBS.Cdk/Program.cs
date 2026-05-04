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

var network = new NetworkStack(app, "FsbsNetworkStack", new NetworkStackProps { Env = env });

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
    DeployEnv = deployEnv
});
appStack.AddDependency(data);

app.Synth();
