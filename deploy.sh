#!/usr/bin/env bash
set -euo pipefail

# ── Staging deploy constants ──────────────────────────────────────────────────
ACCOUNT_ID="679777944071"
REGION="eu-west-1"
WAF_REGION="us-east-1"
DOMAIN="fsbs.tqaentry.com"
ROOT_TENANT_ID="f98e1104-fb79-4273-91cc-24165ebae395"
ENTRA_CLIENT_ID="7c4d9b67-713b-4bf6-bb58-2a096590d574"
ENTRA_TENANT_ID="ad999378-23c8-46ed-9254-c191aae0fc77"
CF_PREFIX_LIST="pl-4fa04526"
ECR_BASE="${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com"
API_IMAGE="${ECR_BASE}/fsbs-api:latest"
WORKER_IMAGE="${ECR_BASE}/fsbs-worker:latest"
CDK_DIR="infrastructure/FSBS.Cdk"
OUTPUTS_FILE="deploy-outputs.env"
CDK_LOG="cdk-deploy.log"

# entraClientSecret is NOT passed on the CLI — CDK reads it via a Secrets Manager
# dynamic reference. Ensure fsbs/entra/client-secret exists before running.

GREEN='\033[0;32m'
YELLOW='\033[0;33m'
RED='\033[0;31m'
NC='\033[0m'

log_info()  { echo -e "${GREEN}[INFO]${NC}  $1"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC}  $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1" >&2; exit 1; }

require_command() {
  command -v "$1" >/dev/null 2>&1 || log_error "Required command '$1' is not installed."
}

CDK_PID=""
SSM_PID=""

cleanup() {
  if [[ -n "$SSM_PID" ]] && kill -0 "$SSM_PID" 2>/dev/null; then
    log_info "Closing SSM tunnel (PID ${SSM_PID})..."
    kill "$SSM_PID" 2>/dev/null || true
  fi
  if [[ -n "$CDK_PID" ]] && kill -0 "$CDK_PID" 2>/dev/null; then
    log_warn "CDK deploy still running (PID ${CDK_PID}) — sending SIGTERM."
    kill "$CDK_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT

# ── Prerequisites ─────────────────────────────────────────────────────────────

require_command aws
require_command cdk
require_command dotnet
require_command jq

command -v session-manager-plugin >/dev/null 2>&1 || {
  log_error "session-manager-plugin is not installed (required for SSM port forwarding).
  Install it with:
    brew install --cask session-manager-plugin
  or download from:
    https://docs.aws.amazon.com/systems-manager/latest/userguide/session-manager-working-with-install-plugin.html"
}

# ── Verify AWS identity ───────────────────────────────────────────────────────

log_info "Verifying AWS credentials..."
ACTUAL_ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
[[ "$ACTUAL_ACCOUNT" == "$ACCOUNT_ID" ]] \
  || log_error "Expected account ${ACCOUNT_ID} but got ${ACTUAL_ACCOUNT}. Run 'aws configure' and try again."
log_info "Authenticated as account ${ACTUAL_ACCOUNT}."

# ── Verify Entra client secret exists in Secrets Manager ─────────────────────

log_info "Checking that fsbs/entra/client-secret exists in Secrets Manager..."
aws secretsmanager describe-secret \
  --secret-id fsbs/entra/client-secret \
  --region "$REGION" \
  --query Name \
  --output text >/dev/null \
  || log_error "fsbs/entra/client-secret not found. Re-run configure_entra_fsbs.sh with --write-aws-secrets."
log_info "Secret found."


# ── CDK deploy — pass 1 (skipDbGrants) ───────────────────────────────────────

CDK_ARGS=(
  --require-approval never
  -c deployEnv=staging
  -c skipDbGrants=true
  -c rootDomain="$DOMAIN"
  -c apiImageUri="$API_IMAGE"
  -c workerImageUri="$WORKER_IMAGE"
  -c rootTenantId="$ROOT_TENANT_ID"
  -c entraClientId="$ENTRA_CLIENT_ID"
  -c entraTenantId="$ENTRA_TENANT_ID"
  -c cloudFrontPrefixListId="$CF_PREFIX_LIST"
)

# Deploy AppStack first in isolation to clear any stale Fn::ImportValue references
# left behind by CDK refactors that changed which DataStack outputs AppStack consumes.
# --exclusively skips dependency stacks so DataStack is not attempted here.
log_info "Pre-deploying FsbsAppStack (--exclusively) to clear stale cross-stack references..."
(cd "$CDK_DIR" && cdk deploy FsbsAppStack --exclusively "${CDK_ARGS[@]}") \
  >"$CDK_LOG" 2>&1
log_info "FsbsAppStack pre-deploy complete."

log_info "Starting CDK deploy --all (pass 1, skipDbGrants=true) in background..."
log_info "Streaming CDK output to ${CDK_LOG} — run 'tail -f ${CDK_LOG}' in another terminal to follow."

(cd "$CDK_DIR" && cdk deploy --all "${CDK_ARGS[@]}") \
  >"$CDK_LOG" 2>&1 &
CDK_PID=$!

# ── Poll for ACM cert CNAME (cert is in us-east-1) ───────────────────────────

log_info "Polling for ACM certificate validation record (this may take a minute)..."
CERT_ARN=""
for i in $(seq 1 40); do
  CERT_ARN=$(aws acm list-certificates \
    --region "$WAF_REGION" \
    --query "CertificateSummaryList[?DomainName=='*.${DOMAIN}'].CertificateArn" \
    --output text 2>/dev/null || true)
  [[ -n "$CERT_ARN" ]] && break
  sleep 15
done

[[ -n "$CERT_ARN" ]] || log_error "ACM certificate was not created within 10 minutes. Check ${CDK_LOG}."

CNAME_NAME=""
CNAME_VALUE=""
for i in $(seq 1 20); do
  RECORD_JSON=$(aws acm describe-certificate \
    --region "$WAF_REGION" \
    --certificate-arn "$CERT_ARN" \
    --query "Certificate.DomainValidationOptions[0].ResourceRecord" \
    --output json 2>/dev/null || echo "null")
  if [[ "$RECORD_JSON" != "null" && "$RECORD_JSON" != "" ]]; then
    CNAME_NAME=$(echo "$RECORD_JSON" | jq -r '.Name // empty' | sed 's/\.$//')
    CNAME_VALUE=$(echo "$RECORD_JSON" | jq -r '.Value // empty' | sed 's/\.$//')
    [[ -n "$CNAME_NAME" ]] && break
  fi
  sleep 15
done

[[ -n "$CNAME_NAME" ]] || log_error "ACM validation record not available yet. Check ${CDK_LOG}."

CERT_STATUS=$(aws acm describe-certificate \
  --region "$WAF_REGION" \
  --certificate-arn "$CERT_ARN" \
  --query "Certificate.Status" \
  --output text 2>/dev/null || echo "")

if [[ "$CERT_STATUS" == "ISSUED" ]]; then
  log_info "ACM certificate is already ISSUED — skipping CNAME prompt."
else
  RESOLVED=$(dig +short "${CNAME_NAME}." 2>/dev/null | head -1 | sed 's/\.$//')
  if [[ "$RESOLVED" == "$CNAME_VALUE" ]]; then
    log_info "ACM validation CNAME already present in DNS — no action needed."
  else
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "  ADD THIS ACM VALIDATION CNAME TO YOUR tqaentry.com NAMESERVERS"
    echo "  (the CDK deploy is paused waiting for cert validation)"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    printf "  %-8s  %-55s  %s\n" "CNAME" "${CNAME_NAME}" "${CNAME_VALUE}"
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    read -r -p "Press Enter once you have added the CNAME (ACM usually validates within 1-5 minutes)..." || true
  fi
fi

# ── Wait for CDK deploy pass 1 to complete ────────────────────────────────────

log_info "Waiting for CDK deploy pass 1 to complete (following ${CDK_LOG})..."
wait "$CDK_PID" || log_error "CDK deploy pass 1 failed. Check ${CDK_LOG} for details."
CDK_PID=""
log_info "CDK deploy pass 1 complete."

# ── Capture stack outputs ─────────────────────────────────────────────────────

log_info "Reading stack outputs..."

CDN_DOMAIN=$(aws cloudformation describe-stacks \
  --stack-name FsbsAppStack \
  --region "$REGION" \
  --query "Stacks[0].Outputs[?OutputKey=='CdnDomain'].OutputValue" \
  --output text)
STAFF_POOL_ID=$(aws cloudformation describe-stacks \
  --stack-name FsbsAppStack \
  --region "$REGION" \
  --query "Stacks[0].Outputs[?OutputKey=='StaffPoolId'].OutputValue" \
  --output text)
STAFF_CLIENT_ID=$(aws cloudformation describe-stacks \
  --stack-name FsbsAppStack \
  --region "$REGION" \
  --query "Stacks[0].Outputs[?OutputKey=='StaffPoolClientId'].OutputValue" \
  --output text)
CUSTOMER_CLIENT_ID=$(aws cloudformation describe-stacks \
  --stack-name FsbsAppStack \
  --region "$REGION" \
  --query "Stacks[0].Outputs[?OutputKey=='CustomerPoolClientId'].OutputValue" \
  --output text)
STAFF_POOL_DOMAIN=$(aws cloudformation describe-stacks \
  --stack-name FsbsAppStack \
  --region "$REGION" \
  --query "Stacks[0].Outputs[?OutputKey=='StaffPoolDomain'].OutputValue" \
  --output text)

# Extract prefix from the full Cognito domain URL
COGNITO_DOMAIN_PREFIX=$(echo "$STAFF_POOL_DOMAIN" \
  | sed 's|https://||' \
  | cut -d'.' -f1)

cat > "$OUTPUTS_FILE" <<EOF
CDN_DOMAIN=${CDN_DOMAIN}
STAFF_POOL_ID=${STAFF_POOL_ID}
STAFF_CLIENT_ID=${STAFF_CLIENT_ID}
CUSTOMER_CLIENT_ID=${CUSTOMER_CLIENT_ID}
COGNITO_DOMAIN_PREFIX=${COGNITO_DOMAIN_PREFIX}
STAFF_POOL_DOMAIN=${STAFF_POOL_DOMAIN}
EOF
log_info "Outputs written to ${OUTPUTS_FILE}."

# ── Fetch Cognito staff client secret ────────────────────────────────────────
# Cognito generates the client secret when GenerateSecret=true.
# Fetched here and passed to CDK pass 2 via context (-c staffClientSecret=...).
# Pass 1 ran without it (empty string); the API starts fine because the secret
# is only used at request time during the OAuth token exchange, not at startup.

log_info "Fetching Cognito staff client secret from pool ${STAFF_POOL_ID} / client ${STAFF_CLIENT_ID}..."
STAFF_COGNITO_SECRET=$(aws cognito-idp describe-user-pool-client \
  --user-pool-id "$STAFF_POOL_ID" \
  --client-id "$STAFF_CLIENT_ID" \
  --region "$REGION" \
  --query "UserPoolClient.ClientSecret" \
  --output text)
[[ -n "$STAFF_COGNITO_SECRET" && "$STAFF_COGNITO_SECRET" != "None" ]] \
  || log_error "Could not retrieve staff pool client secret. Ensure GenerateSecret=true in AppStack."
log_info "Staff client secret retrieved."

# ── Get RDS endpoint ──────────────────────────────────────────────────────────

log_info "Resolving RDS endpoint..."
RDS_ENDPOINT=$(aws rds describe-db-instances \
  --region "$REGION" \
  --query "DBInstances[?DBName=='fsbs'].Endpoint.Address" \
  --output text)
[[ -n "$RDS_ENDPOINT" ]] || log_error "Could not find RDS instance with DBName 'fsbs'."
log_info "RDS endpoint: ${RDS_ENDPOINT}"

# ── Open SSM port-forward tunnel ──────────────────────────────────────────────

log_info "Locating a running ECS API task..."
TASK_ARN=$(aws ecs list-tasks \
  --cluster fsbs \
  --service-name fsbs-api \
  --region "$REGION" \
  --query "taskArns[0]" \
  --output text)
[[ -n "$TASK_ARN" && "$TASK_ARN" != "None" ]] || log_error "No running tasks found in service fsbs-api."
TASK_ID="${TASK_ARN##*/}"
log_info "Using task ${TASK_ID}."

log_info "Resolving container runtime ID..."
CONTAINER_RUNTIME_ID=$(aws ecs describe-tasks \
  --cluster fsbs \
  --tasks "$TASK_ARN" \
  --region "$REGION" \
  --query "tasks[0].containers[0].runtimeId" \
  --output text)
[[ -n "$CONTAINER_RUNTIME_ID" && "$CONTAINER_RUNTIME_ID" != "None" ]] \
  || log_error "Could not retrieve container runtime ID for task ${TASK_ID}."
log_info "Container runtime ID: ${CONTAINER_RUNTIME_ID}"

log_info "Opening SSM port-forward tunnel to RDS (localhost:5433 → ${RDS_ENDPOINT}:5432)..."
aws ssm start-session \
  --target "ecs:fsbs_${TASK_ID}_${CONTAINER_RUNTIME_ID}" \
  --document-name AWS-StartPortForwardingSessionToRemoteHost \
  --parameters "host=${RDS_ENDPOINT},portNumber=5432,localPortNumber=5433" \
  --region "$REGION" &
SSM_PID=$!

log_info "Waiting for tunnel to be ready..."
for i in $(seq 1 12); do
  if nc -z localhost 5433 2>/dev/null; then
    log_info "Tunnel is ready."
    break
  fi
  if [[ $i -eq 12 ]]; then
    log_error "SSM tunnel did not establish within 60 seconds."
  fi
  sleep 5
done

# ── Apply EF Core migrations ──────────────────────────────────────────────────

log_info "Applying EF Core migrations via SSM tunnel..."
MASTER_PASSWORD=$(aws secretsmanager get-secret-value \
  --secret-id fsbs/rds/master \
  --region "$REGION" \
  --query SecretString \
  --output text | jq -r '.password')

FSBS_DB_HOST=localhost \
FSBS_DB_PORT=5433 \
FSBS_DB_NAME=fsbs \
FSBS_DB_USERNAME=fsbs_master \
FSBS_DB_PASSWORD="$MASTER_PASSWORD" \
dotnet ef database update \
  --project src/FSBS.Infrastructure.Persistence.Migrations \
  --startup-project src/FSBS.Infrastructure.Persistence.Migrations

log_info "Migrations applied."

kill "$SSM_PID" 2>/dev/null || true
SSM_PID=""

# ── CDK deploy — pass 2 (with DB grants) ─────────────────────────────────────

log_info "Starting CDK deploy pass 2 (DB grants enabled)..."
CDK_ARGS_PASS2=(
  --require-approval never
  -c deployEnv=staging
  -c rootDomain="$DOMAIN"
  -c apiImageUri="$API_IMAGE"
  -c workerImageUri="$WORKER_IMAGE"
  -c rootTenantId="$ROOT_TENANT_ID"
  -c entraClientId="$ENTRA_CLIENT_ID"
  -c entraTenantId="$ENTRA_TENANT_ID"
  -c cloudFrontPrefixListId="$CF_PREFIX_LIST"
  -c staffClientSecret="$STAFF_COGNITO_SECRET"
)
(cd "$CDK_DIR" && cdk deploy FsbsAppStack "${CDK_ARGS_PASS2[@]}") \
  >>"$CDK_LOG" 2>&1
log_info "CDK deploy pass 2 complete."

# ── Final instructions ────────────────────────────────────────────────────────

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  CDK DEPLOY COMPLETE — TWO MANUAL STEPS REMAIN"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "  1. Add the app subdomain CNAME to your tqaentry.com nameservers:"
printf "     %-8s  %-40s  %s\n" "CNAME" "staging.${DOMAIN}" "${CDN_DOMAIN}"
echo ""
echo "  2. Run upload-frontend.sh to configure and deploy the Blazor WASM app."
echo ""
echo "  Stack outputs are saved to ${OUTPUTS_FILE}."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
