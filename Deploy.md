# Deploying FSBS to AWS

The infrastructure is fully defined in `infrastructure/FSBS.Cdk` using AWS CDK (C#). Follow these steps in order.

---

## Prerequisites

Install and configure the following on your machine:

- **AWS CLI** — authenticated with credentials that have sufficient IAM permissions
- **AWS CDK CLI** — `npm install -g aws-cdk` (v2)
- **.NET 10 SDK** — required to build and run the CDK app
- **Docker** — required to build and push the API and Worker container images
- **Node.js** — required by the CDK CLI

Set your target account and region:

```bash
export CDK_DEFAULT_ACCOUNT=123456789012
export CDK_DEFAULT_REGION=eu-west-1
```

---

## Step 1 — Bootstrap the AWS environment

This only needs to be done once per account/region:

```bash
cdk bootstrap aws://$CDK_DEFAULT_ACCOUNT/$CDK_DEFAULT_REGION
```

---

## Step 2 — Build and push Docker images

The CDK `AppStack` requires pre-built container images for the API and Worker. Build and push them to ECR (or any container registry):

```bash
# Authenticate Docker to ECR
aws ecr get-login-password --region $CDK_DEFAULT_REGION | \
  docker login --username AWS --password-stdin $CDK_DEFAULT_ACCOUNT.dkr.ecr.$CDK_DEFAULT_REGION.amazonaws.com

# Create ECR repos if they don't exist
aws ecr create-repository --repository-name fsbs-api --region $CDK_DEFAULT_REGION
aws ecr create-repository --repository-name fsbs-worker --region $CDK_DEFAULT_REGION

# Build and push API
docker build -t fsbs-api -f src/FSBS.Api/Dockerfile .
docker tag fsbs-api:latest $CDK_DEFAULT_ACCOUNT.dkr.ecr.$CDK_DEFAULT_REGION.amazonaws.com/fsbs-api:latest
docker push $CDK_DEFAULT_ACCOUNT.dkr.ecr.$CDK_DEFAULT_REGION.amazonaws.com/fsbs-api:latest

# Build and push Worker
docker build -t fsbs-worker -f src/FSBS.Worker/Dockerfile .
docker tag fsbs-worker:latest $CDK_DEFAULT_ACCOUNT.dkr.ecr.$CDK_DEFAULT_REGION.amazonaws.com/fsbs-worker:latest
docker push $CDK_DEFAULT_ACCOUNT.dkr.ecr.$CDK_DEFAULT_REGION.amazonaws.com/fsbs-worker:latest
```

Note the full image URIs — you'll need them in the next step.

---

## Step 3 — Build the Blazor WASM frontend

The Blazor WASM output is deployed to the `fsbs-static` S3 bucket (served via CloudFront). Build it before deploying:

```bash
dotnet publish src/FSBS.Web/FSBS.Web.csproj -c Release -o publish/web
```

---

## Step 3b — Publish Lambda functions

The Cognito trigger Lambdas are packaged from a single published asset. Build it before running CDK:

```bash
dotnet publish src/FSBS.Functions/FSBS.Functions.csproj -c Release \
  -o infrastructure/FSBS.Cdk/.artifacts/functions
```

---

## Step 4 — Deploy the CDK stacks

Run from the CDK project directory:

```bash
cd infrastructure/FSBS.Cdk
```

Three mandatory context values are required at deploy time — the app will throw without them:

| Context key | Description |
|---|---|
| `apiImageUri` | Full ECR URI for the API image (e.g. `123456789012.dkr.ecr.eu-west-1.amazonaws.com/fsbs-api:latest`) |
| `workerImageUri` | Full ECR URI for the Worker image |
| `rootTenantId` | The school's root `tenant_id` GUID (used for staff JWT claims and RLS) |
| `entraClientId` | Azure app registration client ID |
| `entraTenantId` | Azure tenant ID (GUID) |
| `deployEnv` | `staging` / `uat` / `production` (defaults to `staging` if omitted) |
| `cloudFrontPrefixListId` | Optional — CloudFront managed prefix list ID for ALB security group (defaults to `pl-93a247fa` for eu-west-1) |

### Staging deploy example

```bash
cdk deploy --all \
  -c deployEnv=staging \
  -c apiImageUri=123456789012.dkr.ecr.eu-west-1.amazonaws.com/fsbs-api:latest \
  -c workerImageUri=123456789012.dkr.ecr.eu-west-1.amazonaws.com/fsbs-worker:latest \
  -c rootTenantId=<your-root-tenant-guid> \
  -c entraClientId=<azure-app-client-id> \
  -c entraTenantId=<azure-tenant-id>
```

### Production deploy example

Production uses a manual approval gate in CodePipeline and blue/green deployment. Trigger it by merging to `main` (CI/CD handles it), or deploy manually:

```bash
cdk deploy --all \
  -c deployEnv=production \
  -c apiImageUri=123456789012.dkr.ecr.eu-west-1.amazonaws.com/fsbs-api:1.0.0 \
  -c workerImageUri=123456789012.dkr.ecr.eu-west-1.amazonaws.com/fsbs-worker:1.0.0 \
  -c rootTenantId=<your-root-tenant-guid> \
  -c entraClientId=<azure-app-client-id> \
  -c entraTenantId=<azure-tenant-id> \
  -c cloudFrontPrefixListId=pl-xxxxxxxx
```

The stacks deploy in dependency order automatically: `FsbsNetworkStack` → `FsbsDataStack` → `FsbsAppStack`.

---

## Step 5 — Apply the database schema

The CDK `AppStack` includes a **DB grants custom resource** (an in-VPC Lambda) that provisions the `fsbs_app` and `fsbs_readonly` PostgreSQL roles automatically after the `DataStack` is up. However, the schema itself must be applied separately on first deploy:

```bash
# Connect via a bastion or SSM Session Manager to the RDS instance
# Then apply the schema using the superuser role (bypasses RLS)
psql -h <rds-endpoint> -U fsbs_superuser -d fsbs -f fsbs_schema.sql
```

> **Important:** Never use `dotnet ef database update` against production. Schema changes go through the CI/CD migration pipeline only.

---

## Step 6 — Upload the Blazor WASM frontend

After `FsbsAppStack` is deployed, upload the Blazor WASM build to the `fsbs-static` S3 bucket:

```bash
aws s3 sync publish/web/wwwroot s3://fsbs-static-$CDK_DEFAULT_ACCOUNT --delete
aws cloudfront create-invalidation --distribution-id <dist-id> --paths "/*"
```

---

## Step 7 — Configure Microsoft Entra ID (Staff Pool)

In the Azure portal, register FSBS as an app registration and:

1. Set the redirect URI to `https://<cognito-domain>/oauth2/idpresponse`
2. Enable the `groups` claim in the token configuration
3. Store the client secret in Secrets Manager under the name `fsbs/entra/client-secret`

The CDK creates the Cognito OIDC IdP (`EntraID`) automatically using the `entraClientId` and `entraTenantId` context values. The `PostConfirmation` Lambda handles group assignment on first sign-in.

---

## Step 8 — Verify SES sending identity

SES starts in sandbox mode. Before sending emails:

```bash
# Verify your sending domain
aws ses verify-domain-identity --domain fsbs.example.com --region $CDK_DEFAULT_REGION
```

Request production access via the AWS console if you need to send to unverified addresses.

---

## Environment summary

| Env | Branch | How to deploy |
|---|---|---|
| Development | `feature/*` | `docker-compose up` (local PostgreSQL only) |
| Staging | `develop` | Auto-deploy on merge via GitHub Actions |
| UAT | `release/*` | Auto-deploy on merge; uses anonymised prod snapshot |
| Production | `main` | Merge to `main` → manual approval gate in CodePipeline → blue/green |

---

## Quick reference — deploy a single stack

```bash
cdk deploy FsbsNetworkStack -c ...   # network only
cdk deploy FsbsDataStack -c ...      # data layer only
cdk deploy FsbsAppStack -c ...       # app layer only
cdk synth                            # synthesise CloudFormation without deploying
cdk diff                             # preview changes before deploying
```
