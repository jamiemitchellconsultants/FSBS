using Amazon.CDK.AWS.Lambda;

namespace FSBS.Cdk.Lambdas;

internal static class FunctionsAsset
{
    // Single packaged asset reused by all Cognito trigger functions.
    // The CDK project build publishes FSBS.Functions to this folder before synth.
    private const string PublishedFunctionsPath = ".artifacts/functions";

    private static readonly Code SharedCode = Code.FromAsset(PublishedFunctionsPath);

    public static Code Code => SharedCode;
}
