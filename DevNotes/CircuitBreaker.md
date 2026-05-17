# ECS Circuit Breaker - Step 16 Staging Deploy

## Incident
`cdk deploy` for `FsbsAppStack` (Step 16 in `DevNotes/StagingRunbook.md`) failed with:

- `AWS::ECS::Service ApiServiceC9037CF0`
- `ECS Deployment Circuit Breaker was triggered`

## What this means
ECS started a new deployment for `fsbs-api`, but tasks did not become healthy in time.
Because deployment circuit breaker rollback is enabled, ECS rolled the service back and CloudFormation marked stack creation as failed.

## Diagnosis summary
CloudFormation showed only the top-level symptom (`ApiService` create failed). The likely startup failure path is configuration mismatch in DB settings:

- API persistence expects `ConnectionStrings:Fsbs`.
- CDK injects `ConnectionStrings__Default` from `fsbs/rds/app`.
- `fsbs/rds/app` is a JSON secret (`username`/`password`), not a full PostgreSQL connection string.
- Worker has the same pattern issue (`Database__ConnectionString` set from full JSON secret).

This causes container startup failure, then ALB health checks fail, then ECS circuit breaker triggers.

## Root cause
Mismatch between:

1. **What app code expects**
   - API: `ConnectionStrings:Fsbs`
   - Worker: `Database:ConnectionString`

2. **What ECS task config provides**
   - API: `ConnectionStrings__Default` from JSON secret
   - Worker: `Database__ConnectionString` from JSON secret

3. **Secret shape**
   - `fsbs/rds/app` contains JSON fields like `username` and `password`, not a complete connection string.

## Fix
Use split DB settings for containers (`Database__Host`, `Database__Port`, `Database__Name`, `Database__Username`, `Database__Password`) and build the connection string in code when a direct connection string is not present.

### 1) `infrastructure/FSBS.Cdk/Stacks/AppStack.cs`
For API container:

- add env vars:
  - `Database__Host`
  - `Database__Port`
  - `Database__Name`
- replace secret mapping:
  - remove `ConnectionStrings__Default`
  - add `Database__Username` from `AppDbSecret` key `username`
  - add `Database__Password` from `AppDbSecret` key `password`

For worker container:

- add same `Database__Host/Port/Name` env vars
- replace `Database__ConnectionString` with:
  - `Database__Username` from `AppDbSecret` key `username`
  - `Database__Password` from `AppDbSecret` key `password`

### 2) `src/FSBS.Infrastructure.Persistence/PersistenceServiceExtensions.cs`
Add fallback logic:

- First try `ConnectionStrings:Fsbs`.
- If missing, compose connection string from:
  - `Database:Host`
  - `Database:Port`
  - `Database:Name`
  - `Database:Username`
  - `Database:Password`

### 3) `src/FSBS.Worker/Program.cs`
Add similar fallback:

- First try `Database:ConnectionString`.
- If missing, build from `Database:Host/Port/Name/Username/Password`.

## Retry deploy (Step 16)
Run from CDK directory:

```bash
cd /Users/jamesmitchell/RiderProjects/FSBS/infrastructure/FSBS.Cdk

cdk deploy FsbsAppStack \
  -c deployEnv=staging \
  -c skipDbGrants=true \
  -c rootDomain=fsbs.tqaentry.com \
  -c apiImageUri=679777944071.dkr.ecr.eu-west-1.amazonaws.com/fsbs-api:latest \
  -c workerImageUri=679777944071.dkr.ecr.eu-west-1.amazonaws.com/fsbs-worker:latest \
  -c rootTenantId=f98e1104-fb79-4273-91cc-24165ebae395 \
  -c entraClientId=7c4d9b67-713b-4bf6-bb58-2a096590d574 \
  -c entraTenantId=ad999378-23c8-46ed-9254-c191aae0fc77 \
  -c entraClientSecret=<client-secret-value-from-step-6> \
  -c cloudFrontPrefixListId=<cloudfront-prefix-list-id>
```

## Quick verification commands
If deploy still fails, collect details immediately:

```bash
AWS_PAGER="" aws cloudformation describe-stack-events \
  --stack-name FsbsAppStack \
  --region eu-west-1 \
  --query "StackEvents[?ResourceStatus=='CREATE_FAILED' || ResourceStatus=='UPDATE_FAILED'].[Timestamp,LogicalResourceId,ResourceStatusReason]" \
  --output table
```

```bash
AWS_PAGER="" aws ecs describe-services \
  --cluster fsbs \
  --services fsbs-api \
  --region eu-west-1 \
  --query 'services[0].events[0:20].[createdAt,message]' \
  --output table
```

```bash
AWS_PAGER="" aws logs tail /fsbs/api --region eu-west-1 --since 30m
```

## Preventive note
Keep DB secret mapping consistent across API, worker, and CDK:

- Prefer structured DB settings (host/port/name/username/password).
- Only use `ConnectionStrings:*` when you actually store a full connection string value.

