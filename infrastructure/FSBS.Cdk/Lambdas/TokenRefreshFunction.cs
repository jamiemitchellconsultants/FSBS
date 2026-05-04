using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace FSBS.Cdk.Lambdas;

/// <summary>
/// Re-syncs Cognito group membership from Entra ID groups (Staff Pool only).
/// Calls AdminUserGlobalSignOut when the Entra account is disabled.
/// </summary>
public class TokenRefreshFunction : Function
{
    public TokenRefreshFunction(Construct scope, string id) : base(scope, id, new FunctionProps
    {
        Runtime = Runtime.DOTNET_8,
        Handler = "FSBS.Functions::FSBS.Functions.TokenRefresh.Function::FunctionHandler",
        Code = Code.FromAsset("src/FSBS.Functions/TokenRefresh/publish"),
        Timeout = Duration.Seconds(15),
        Description = "Cognito Token Refresh: re-syncs Entra groups, signs out disabled accounts"
    })
    { }
}
