using Amazon.CDK;
using Amazon.CDK.AWS.WAFv2;
using Constructs;

namespace FSBS.Cdk.Stacks;

public class WafStack : Stack
{
    public string WebAclArn { get; }

    public WafStack(Construct scope, string id, StackProps props) : base(scope, id, props)
    {
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
                    OverrideAction = new CfnWebACL.OverrideActionProperty { None = new Dictionary<string, object>() },
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
                    OverrideAction = new CfnWebACL.OverrideActionProperty { None = new Dictionary<string, object>() },
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

        WebAclArn = wafAcl.AttrArn;
    }
}
