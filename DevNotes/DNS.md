# DNS Records for fsbs.tqaentry.com

All records are added to the `tqaentry.com` zone on your own nameservers.

---

## 1 — ACM certificate validation (one-time, covers all environments)

The CDK creates a single wildcard certificate `*.fsbs.tqaentry.com` that covers
staging, UAT, and production. ACM requires a DNS validation CNAME before it will
issue the certificate.

Get the exact name and value from **AWS Console → Certificate Manager → your cert →
Domains** immediately after the first `cdk deploy` starts (the deploy will hang
until the cert is issued).

| Type | Name | Value |
|---|---|---|
| CNAME | `_<hash>.fsbs.tqaentry.com` | `_<hash>.acm-validations.aws.` |

This record only needs to be added once — it validates all three environment
subdomains via the wildcard.

---

## 2 — Application subdomains

Each environment has its own CloudFront distribution. The CNAME value is the
`CdnDomain` output printed by `cdk deploy` for that environment.

| Type | Name | Value | Environment |
|---|---|---|---|
| CNAME | `staging.fsbs.tqaentry.com` | `<staging CdnDomain>.cloudfront.net` | Staging |
| CNAME | `uat.fsbs.tqaentry.com` | `<uat CdnDomain>.cloudfront.net` | UAT |
| CNAME | `app.fsbs.tqaentry.com` | `<prod CdnDomain>.cloudfront.net` | Production |

Add each record after its environment's `cdk deploy` completes and the `CdnDomain`
output is available.

To retrieve the value after deploy:

```bash
aws cloudformation describe-stacks \
  --stack-name FsbsAppStack \
  --region eu-west-1 \
  --query "Stacks[0].Outputs[?OutputKey=='CdnDomain'].OutputValue" \
  --output text
```

---

## 3 — SES email sending

Required if you want to send email from `@fsbs.tqaentry.com`. Run these commands
to get the token values:

```bash
aws ses verify-domain-identity --domain fsbs.tqaentry.com --region eu-west-1
aws ses verify-domain-dkim --domain fsbs.tqaentry.com --region eu-west-1
```

| Type | Name | Value |
|---|---|---|
| TXT | `_amazonses.fsbs.tqaentry.com` | `<verification token from verify-domain-identity>` |
| CNAME | `<token1>._domainkey.fsbs.tqaentry.com` | `<token1>.dkim.amazonses.com` |
| CNAME | `<token2>._domainkey.fsbs.tqaentry.com` | `<token2>.dkim.amazonses.com` |
| CNAME | `<token3>._domainkey.fsbs.tqaentry.com` | `<token3>.dkim.amazonses.com` |

These can be added any time before email sending is required.

---

## Summary — order of operations

1. Run `cdk deploy` for the first environment
2. Add the ACM validation CNAME immediately — the deploy will hang until the cert is issued
3. Once the cert is issued the deploy will continue to completion
4. Copy the `CdnDomain` output and add the corresponding app subdomain CNAME
5. Repeat steps 1 and 4 for each subsequent environment (no new ACM record needed)
6. Add SES records when email sending is needed
