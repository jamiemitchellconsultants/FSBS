# FSBS.Cdk

AWS CDK application written in C#. Defines and deploys all cloud infrastructure for FSBS.

## Responsibilities

Three stacks under `Stacks/`:

| Stack | Contents |
|---|---|
| `NetworkStack` | VPC (3 AZs), public/private/isolated subnets, NAT Gateway per AZ, security groups |
| `DataStack` | RDS PostgreSQL Multi-AZ, ElastiCache Redis, S3 buckets (`fsbs-static`, `fsbs-documents`), Secrets Manager entries |
| `AppStack` | ECS Fargate (API + worker services), CloudFront + WAF, ALB, Cognito pools (Staff + Customer), SQS/SNS topics, SES, ACM wildcard cert, CloudWatch dashboards + alarms |

Cognito Lambda triggers under `Lambdas/`:

| Lambda | Purpose |
|---|---|
| `PreSignUpFunction` | Validates SHA-256 invitation token hash before allowing Customer Pool sign-up |
| `PostConfirmationFunction` | Creates `AppUser`, assigns `org_id`/`app_role`, marks invitation Claimed, places staff in Cognito group |
| `TokenRefreshFunction` | Re-syncs Cognito group membership from Entra ID groups (Staff Pool only); calls `AdminUserGlobalSignOut` when Entra account is disabled |

## Key resource specs

- ECS Fargate API: 1 vCPU / 2 GB RAM, min 2 / max 10 tasks, CPU scale-out at 60%
- RDS: db.t4g.medium, Multi-AZ, 100 GB gp3, 7-day PITR
- ElastiCache: cache.t4g.small, TLS in-transit
- WAF: rate limit 300 req/5 min per IP, OWASP Core Rule Set
- ALB: inbound only from CloudFront managed prefix list

## Deployment environments

| Env | Branch | DB |
|---|---|---|
| Development | `feature/*` | Local Docker PostgreSQL |
| Staging | `develop` | RDS single-AZ t4g.micro |
| UAT | `release/*` | RDS single-AZ t4g.small |
| Production | `main` | RDS Multi-AZ t4g.medium (manual approval gate, blue/green) |

## Do not add

- Application code, domain logic, or API handlers
- Long-lived AWS credentials — use Secrets Manager and IAM roles only
