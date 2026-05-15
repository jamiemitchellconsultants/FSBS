using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Constructs;

namespace FSBS.Cdk.Stacks;

public class CertStackProps : StackProps
{
    public required string RootDomain { get; init; }
}

public class CertStack : Stack
{
    public ICertificate Certificate { get; }

    public CertStack(Construct scope, string id, CertStackProps props) : base(scope, id, props)
    {
        Certificate = new Certificate(this, "WildcardCert", new CertificateProps
        {
            DomainName = $"*.{props.RootDomain}",
            Validation = CertificateValidation.FromDns()
        });
    }
}
