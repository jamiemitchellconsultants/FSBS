# Staging Deployment Runbook

End-to-end steps to get the staging stack running with a staff user able to sign
in through the Blazor WASM UI.

- AWS Account ID: `679777944071`
- AWS Region: `eu-west-1`
- Azure Tenant ID: `ad999378-23c8-46ed-9254-c191aae0fc77`
- Root domain: `fsbs.tqaentry.com`
- App URL (staging): `https://staging.fsbs.tqaentry.com`

---

## Prerequisites — install tooling

Install the following on your machine before starting:

```bash
# AWS CLI v2
# https://docs.aws.amazon.com/cli/latest/userguide/install-cliv2.html

# Node.js (required by CDK CLI)
# https://nodejs.org

# AWS CDK CLI v2
npm install -g aws-cdk

# .NET 10 SDK
# https://dotnet.microsoft.com/download

# Docker Desktop
# https://www.docker.com/products/docker-desktop

# Azure CLI
# https://learn.microsoft.com/en-us/cli/azure/install-azure-cli

# jq
brew install jq   # macOS
```

---

## Phase 1 — AWS account setup

### Before you start — create an IAM user and access key

You need programmatic access credentials for the AWS CLI and CDK. The recommended
approach is to create a dedicated IAM user with the minimum permissions required
rather than using root account credentials.

1. Sign in to the **AWS Console** at https://console.aws.amazon.com with your
   root or admin account

2. Go to **IAM → Users → Create user**

3. Set the user name to `fsbs-deploy` and click **Next**

4. On the permissions page, select **Attach policies directly** and attach the
   following AWS managed policies:
   - `AdministratorAccess` — required for CDK to create and manage all resources
     (this can be scoped down after the initial deploy if desired)

5. Click through to **Create user**

6. Open the newly created user, go to the **Security credentials** tab, and click
   **Create access key**

7. Select **Command Line Interface (CLI)** as the use case, tick the confirmation
   checkbox, and click **Next**

8. Add a description tag such as `fsbs-deploy-local` and click **Create access key**

9. **Copy both the Access Key ID and Secret Access Key now** — the secret is only
   shown once and cannot be retrieved again. Store them in a password manager.

10. Click **Done**

> If your organisation uses AWS IAM Identity Center (SSO) instead of IAM users,
> run `aws configure sso` instead of `aws configure` in Step 1 and follow the
> browser-based login flow. The rest of the runbook is the same.

### Step 1 — Configure AWS credentials

```bash
aws configure
# AWS Access Key ID: <your key>
# AWS Secret Access Key: <your secret>
# Default region name: eu-west-1
# Default output format: json
```

Verify:

```bash
aws sts get-caller-identity
# Should show account 679777944071
```

### Step 2 — Set environment variables

Add these to your shell profile or run them in every terminal session:

```bash
export CDK_DEFAULT_ACCOUNT=679777944071
export CDK_DEFAULT_REGION=eu-west-1
```

### Step 3 — Bootstrap CDK (one-time)

```bash
cdk bootstrap aws://679777944071/eu-west-1
```

---

## Phase 2 — Entra ID app registration

This phase configures the Azure side. The script handles everything automatically.

### Step 4 — Log in to Azure CLI

```bash
az login
# A browser window will open — sign in with your Entra admin account
# Confirm the active tenant is ad999378-23c8-46ed-9254-c191aae0fc77:
az account show --query tenantId -o tsv
```

### Step 5 — Dry-run the Entra script

Preview what the script will do without making any changes:

```bash
./configure_entra_fsbs.sh \
  --cognito-domain fsbs-staff.auth.eu-west-1.amazoncognito.com \
  --tenant-id ad999378-23c8-46ed-9254-c191aae0fc77 \
  --dry-run
```

> The Cognito domain prefix `fsbs-staff` is a placeholder at this point — the
> actual domain is created in Phase 5. The script only uses it to construct the
> redirect URI, so the dry-run output is still useful for review.

### Step 6 — Run the Entra script for real

```bash
./configure_entra_fsbs.sh \
  --cognito-domain fsbs-staff.auth.eu-west-1.amazoncognito.com \
  --tenant-id ad999378-23c8-46ed-9254-c191aae0fc77 \
  --write-aws-secrets \
  --aws-region eu-west-1
```

When prompted, type the tenant ID `ad999378-23c8-46ed-9254-c191aae0fc77` to confirm.

The script will print a summary at the end. **Record these values — you will need
them in later steps:**

- Application (client) ID — referred to below as `<entra-client-id>`
- Client secret value — stored automatically in Secrets Manager as
  `fsbs/entra/client-secret` if `--write-aws-secrets` was used, but copy it
  somewhere safe now as it cannot be retrieved again
- Issuer URL: `https://login.microsoftonline.com/ad999378-23c8-46ed-9254-c191aae0fc77/v2.0`

### Step 7 — Assign a staff user to an Entra group

At least one user must be in an Entra group before they can sign in. In the
**Azure Portal → Microsoft Entra ID → Groups**, find the group matching the role
you want (e.g. `SystemAdmin`) and add the user as a member.

Alternatively via CLI:

```bash
# Get the group object ID
GROUP_ID=$(az ad group list --filter "displayName eq 'SystemAdmin'" --query '[0].id' -o tsv)

# Get the user object ID
USER_ID=$(az ad user show --id <user@yourdomain.com> --query id -o tsv)

# Add the user to the group
az ad group member add --group "$GROUP_ID" --member-id "$USER_ID"
```

---

## Phase 3 — Build artefacts

Run all build steps from the repository root.

### Step 8 — Create ECR repositories

```bash
aws ecr create-repository --repository-name fsbs-api --region eu-west-1
aws ecr create-repository --repository-name fsbs-worker --region eu-west-1
```

### Step 9 — Build and push Docker images

```bash
# Authenticate Docker to ECR
aws ecr get-login-password --region eu-west-1 | \
  docker login --username AWS --password-stdin \
  679777944071.dkr.ecr.eu-west-1.amazonaws.com

# Build and push API
docker build -t fsbs-api -f src/FSBS.Api/Dockerfile .
docker tag fsbs-api:latest \
  679777944071.dkr.ecr.eu-west-1.amazonaws.com/fsbs-api:latest
docker push \
  679777944071.dkr.ecr.eu-west-1.amazonaws.com/fsbs-api:latest

# Build and push Worker
docker build -t fsbs-worker -f src/FSBS.Worker/Dockerfile .
docker tag fsbs-worker:latest \
  679777944071.dkr.ecr.eu-west-1.amazonaws.com/fsbs-worker:latest
docker push \
  679777944071.dkr.ecr.eu-west-1.amazonaws.com/fsbs-worker:latest
```

### Step 10 — Publish Lambda functions

```bash
dotnet publish src/FSBS.Functions/FSBS.Functions.csproj -c Release \
  -o infrastructure/FSBS.Cdk/.artifacts/functions
```

### Step 11 — Build the Blazor WASM frontend

```bash
dotnet publish src/FSBS.Web/FSBS.Web.csproj -c Release -o publish/web
```

---

## Phase 4 — SES domain verification and DKIM

Do this before deploying so email works as soon as the stack is up.

### Step 12 — Request SES domain verification

```bash
aws ses verify-domain-identity \
  --domain fsbs.tqaentry.com \
  --region eu-west-1
```

This returns a verification token, e.g.:

```json
{
    "VerificationToken": "pmBGN/7MjnfhTKUZ06Enqq1PeGUaAkiadgNF6I4a1t8="
}
```

Add this TXT record to your `tqaentry.com` nameservers immediately:

| Type | Name | Value |
|---|---|---|
| TXT | `_amazonses.fsbs.tqaentry.com` | `<VerificationToken value>` |

### Step 13 — Request DKIM tokens

```bash
aws ses verify-domain-dkim \
  --domain fsbs.tqaentry.com \
  --region eu-west-1
```

This returns three tokens, e.g.:

```json
{
    "DkimTokens": [
        "vvjuipp7ztpmpd7duaxxx",
        "aabbbcc7ztpmpd7duayyy",
        "ccddee7ztpmpd7duazzz"
    ]
}
```

Add all three CNAME records to your `tqaentry.com` nameservers:

| Type | Name | Value |
|---|---|---|
| CNAME | `vvjuipp7ztpmpd7duaxxx._domainkey.fsbs.tqaentry.com` | `vvjuipp7ztpmpd7duaxxx.dkim.amazonses.com` |
| CNAME | `aabbbcc7ztpmpd7duayyy._domainkey.fsbs.tqaentry.com` | `aabbbcc7ztpmpd7duayyy.dkim.amazonses.com` |
| CNAME | `ccddee7ztpmpd7duazzz._domainkey.fsbs.tqaentry.com` | `ccddee7ztpmpd7duazzz.dkim.amazonses.com` |

### Step 14 — Verify SES domain status

Wait a few minutes for DNS to propagate, then confirm both checks pass:

```bash
aws ses get-identity-verification-attributes \
  --identities fsbs.tqaentry.com \
  --region eu-west-1 \
  --query "VerificationAttributes.\"fsbs.tqaentry.com\".VerificationStatus" \
  --output text
# Expected: Success
```

```bash
aws ses get-identity-dkim-attributes \
  --identities fsbs.tqaentry.com \
  --region eu-west-1 \
  --query "DkimAttributes.\"fsbs.tqaentry.com\".DkimVerificationStatus" \
  --output text
# Expected: Success
```

> SES starts in sandbox mode — it can only send to verified addresses. To send
> to any address, request production access in **AWS Console → SES →
> Account dashboard → Request production access**.

---

## Phase 5 — Deploy CDK stacks

### Step 15 — Choose a root tenant ID

The `rootTenantId` is a GUID that identifies your school's organisation in the
FSBS database. If you don't have one yet, generate one now:

```bash
uuidgen | tr '[:upper:]' '[:lower:]'
# e.g. f47ac10b-58cc-4372-a567-0e02b2c3d479
```

Keep this value — it must remain the same across all future deploys.

### Step 16 — Deploy all stacks

```bash
cd infrastructure/FSBS.Cdk

cdk deploy --all \
  -c deployEnv=staging \
  -c rootDomain=fsbs.tqaentry.com \
  -c apiImageUri=679777944071.dkr.ecr.eu-west-1.amazonaws.com/fsbs-api:latest \
  -c workerImageUri=679777944071.dkr.ecr.eu-west-1.amazonaws.com/fsbs-worker:latest \
  -c rootTenantId=<your-root-tenant-guid> \
  -c entraClientId=<entra-client-id> \
  -c entraTenantId=ad999378-23c8-46ed-9254-c191aae0fc77
```

The deploy will pause waiting for the ACM certificate to be validated. **Do not
cancel it** — proceed to Step 17 immediately in a second terminal.

### Step 17 — Add the ACM validation CNAME (while deploy is running)

In a new terminal:

```bash
aws acm list-certificates --region eu-west-1 \
  --query "CertificateSummaryList[?DomainName=='*.fsbs.tqaentry.com'].CertificateArn" \
  --output text | xargs -I{} \
  aws acm describe-certificate --region eu-west-1 --certificate-arn {} \
  --query "Certificate.DomainValidationOptions[0].ResourceRecord"
```

This outputs something like:

```json
{
    "Name": "_abc123def456.fsbs.tqaentry.com.",
    "Type": "CNAME",
    "Value": "_xyz789.acm-validations.aws."
}
```

Add this CNAME to your `tqaentry.com` nameservers:

| Type | Name | Value |
|---|---|---|
| CNAME | `_abc123def456.fsbs.tqaentry.com` | `_xyz789.acm-validations.aws.` |

Once DNS propagates (usually 1–5 minutes) ACM will validate the cert and the
CDK deploy will continue automatically.

### Step 18 — Record CDK outputs

When the deploy completes, note the following output values:

```bash
aws cloudformation describe-stacks \
  --stack-name FsbsAppStack \
  --region eu-west-1 \
  --query "Stacks[0].Outputs" \
  --output table
```

Record:
- `CdnDomain` — e.g. `d1234abcd.cloudfront.net` — referred to below as `<cdn-domain>`
- `StaffPoolId` — e.g. `eu-west-1_AbCdEfGhI` — referred to below as `<staff-pool-id>`

Also get the staff app client ID:

```bash
aws cognito-idp list-user-pool-clients \
  --user-pool-id <staff-pool-id> \
  --region eu-west-1 \
  --query "UserPoolClients[?ClientName=='fsbs-staff-client'].ClientId" \
  --output text
# referred to below as <staff-client-id>
```

And the Cognito hosted UI domain prefix:

```bash
aws cognito-idp describe-user-pool \
  --user-pool-id <staff-pool-id> \
  --region eu-west-1 \
  --query "UserPool.Domain" \
  --output text
# referred to below as <cognito-domain-prefix>
# Full domain: <cognito-domain-prefix>.auth.eu-west-1.amazoncognito.com
```

---

## Phase 6 — DNS records

### Step 19 — Add the app subdomain CNAME

Add this record to your `tqaentry.com` nameservers:

| Type | Name | Value |
|---|---|---|
| CNAME | `staging.fsbs.tqaentry.com` | `<cdn-domain>` |

---

## Phase 7 — Update the Entra redirect URI

The Entra script used a placeholder Cognito domain in Step 6. Now that the real
Cognito domain is known, update the redirect URI.

### Step 20 — Re-run the Entra script with the real Cognito domain

```bash
./configure_entra_fsbs.sh \
  --cognito-domain <cognito-domain-prefix>.auth.eu-west-1.amazoncognito.com \
  --tenant-id ad999378-23c8-46ed-9254-c191aae0fc77 \
  --skip-secret \
  --skip-groups
```

This updates the redirect URI in the Entra app registration to the real Cognito
`oauth2/idpresponse` endpoint without rotating the secret or recreating groups.

---

## Phase 8 — Apply the database schema

### Step 21 — Get the RDS endpoint

```bash
aws rds describe-db-instances \
  --region eu-west-1 \
  --query "DBInstances[?DBName=='fsbs'].Endpoint.Address" \
  --output text
# referred to below as <rds-endpoint>
```

### Step 22 — Connect to RDS and apply the schema

RDS is in an isolated subnet with no public access. Use SSM Session Manager port
forwarding via an ECS task to connect:

```bash
# Get a running API task ID
TASK_ARN=$(aws ecs list-tasks \
  --cluster fsbs \
  --service-name fsbs-api \
  --region eu-west-1 \
  --query "taskArns[0]" \
  --output text)

# Port-forward RDS through the task
aws ssm start-session \
  --target "ecs:fsbs_${TASK_ARN##*/}" \
  --document-name AWS-StartPortForwardingSessionToRemoteHost \
  --parameters "host=<rds-endpoint>,portNumber=5432,localPortNumber=5433" \
  --region eu-west-1
```

In a second terminal, apply the schema:

```bash
MASTER_PASSWORD=$(aws secretsmanager get-secret-value \
  --secret-id fsbs/rds/master \
  --region eu-west-1 \
  --query SecretString \
  --output text | jq -r '.password')

PGPASSWORD="$MASTER_PASSWORD" psql \
  -h localhost -p 5433 \
  -U fsbs_master -d fsbs \
  -f fsbs_schema.sql
```

---

## Phase 9 — Upload the Blazor WASM frontend

### Step 23 — Update wwwroot/appsettings.json

Edit `src/FSBS.Web/wwwroot/appsettings.json` with the real values from Step 18:

```json
{
  "ApiBaseUrl": "https://staging.fsbs.tqaentry.com",
  "Cognito": {
    "StaffPoolLoginUrl": "https://<cognito-domain-prefix>.auth.eu-west-1.amazoncognito.com/login?client_id=<staff-client-id>&response_type=code&scope=openid+email+profile&redirect_uri=https%3A%2F%2Fstaging.fsbs.tqaentry.com%2Fauth%2Fcallback%2Fstaff"
  }
}
```

Rebuild the frontend after saving:

```bash
dotnet publish src/FSBS.Web/FSBS.Web.csproj -c Release -o publish/web
```

### Step 24 — Upload to S3 and invalidate CloudFront

```bash
DIST_ID=$(aws cloudfront list-distributions \
  --query "DistributionList.Items[?contains(Aliases.Items, 'staging.fsbs.tqaentry.com')].Id" \
  --output text)

aws s3 sync publish/web/wwwroot \
  s3://fsbs-static-679777944071 --delete

aws cloudfront create-invalidation \
  --distribution-id "$DIST_ID" \
  --paths "/*"
```

---

## Phase 10 — Verify the Cognito hosted UI

### Step 25 — Check Cognito hosted UI callback URLs

In **AWS Console → Cognito → User Pools → fsbs-staff-pool →
App clients → fsbs-staff-client → Edit hosted UI**, confirm:

- Allowed callback URLs includes `https://staging.fsbs.tqaentry.com/auth/callback/staff`
- Allowed sign-out URLs includes `https://staging.fsbs.tqaentry.com/logout`
- Identity providers: `EntraID` only
- OAuth 2.0 grant types: `Authorization code`
- OpenID Connect scopes: `openid`, `email`, `profile`

The CDK sets these automatically from the `appDomain` value, so they should
already be correct. If they are missing, update them manually in the console.

---

## Phase 11 — Smoke test

### Step 26 — Sign in as a staff user

1. Navigate to `https://staging.fsbs.tqaentry.com`
2. Click the staff sign-in button — you should be redirected to the Cognito
   hosted UI, then immediately forwarded to the Microsoft sign-in page
3. Sign in with the Entra account you added to a group in Step 7
4. On first sign-in, the PostConfirmation Lambda fires, creates the `app_users`
   row, and assigns the Cognito group
5. You should land on the FSBS dashboard with the correct role displayed

### Step 27 — Verify the API is healthy

```bash
curl -s https://staging.fsbs.tqaentry.com/v1/health
# Expected: 200 OK
```

---

## Troubleshooting

**Deploy hangs after "Creating certificate"**
→ The ACM CNAME has not been added or has not propagated yet. Check with:
```bash
dig _<hash>.fsbs.tqaentry.com CNAME
```

**Sign-in redirects to an error page after Entra**
→ The Entra redirect URI does not match the Cognito domain. Re-run Step 20 with
the correct `--cognito-domain` value.

**PostConfirmation Lambda error / user sees error after sign-in**
→ The user is not a member of any Entra group. Complete Step 7.

**CloudFront returns 403 on the app URL**
→ The CNAME record has not propagated yet, or the S3 upload in Step 24 has not
completed. Check CloudFront distribution status in the console.

**`cdk deploy` fails with "apiImageUri is required"**
→ You are missing a `-c` context flag. Ensure all required context values are
present in the deploy command.

**SES verification status stays "Pending"**
→ The TXT or DKIM CNAME records have not propagated. Check with:
```bash
dig _amazonses.fsbs.tqaentry.com TXT
dig <token>._domainkey.fsbs.tqaentry.com CNAME
```
