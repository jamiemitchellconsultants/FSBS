#!/usr/bin/env bash
set -euo pipefail

ACCOUNT_ID="679777944071"
REGION="eu-west-1"
WAF_REGION="us-east-1"
DOMAIN="fsbs.tqaentry.com"
SES_POLL_TIMEOUT=600

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

# ── Prerequisites ─────────────────────────────────────────────────────────────

require_command aws
require_command cdk
require_command jq

# ── Verify AWS identity ───────────────────────────────────────────────────────

log_info "Verifying AWS credentials..."
ACTUAL_ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
[[ "$ACTUAL_ACCOUNT" == "$ACCOUNT_ID" ]] \
  || log_error "Expected account ${ACCOUNT_ID} but got ${ACTUAL_ACCOUNT}. Run 'aws configure' and try again."
log_info "Authenticated as account ${ACTUAL_ACCOUNT}."

# ── CDK bootstrap ─────────────────────────────────────────────────────────────

log_info "Bootstrapping CDK in ${REGION}..."
cdk bootstrap "aws://${ACCOUNT_ID}/${REGION}"

log_info "Bootstrapping CDK in ${WAF_REGION} (required for CloudFront-scoped WAF)..."
cdk bootstrap "aws://${ACCOUNT_ID}/${WAF_REGION}"

# ── ECR repositories ──────────────────────────────────────────────────────────

log_info "Creating ECR repositories (idempotent)..."
for REPO in fsbs-api fsbs-worker; do
  if aws ecr describe-repositories --repository-names "$REPO" --region "$REGION" >/dev/null 2>&1; then
    log_info "ECR repository '${REPO}' already exists."
  else
    aws ecr create-repository --repository-name "$REPO" --region "$REGION" >/dev/null
    log_info "Created ECR repository '${REPO}'."
  fi
done

# ── SES domain verification ───────────────────────────────────────────────────

log_info "Requesting SES domain identity verification for ${DOMAIN}..."
VERIFY_TOKEN=$(aws ses verify-domain-identity \
  --domain "$DOMAIN" \
  --region "$REGION" \
  --query VerificationToken \
  --output text)

log_info "Requesting SES DKIM tokens..."
DKIM_TOKENS=$(aws ses verify-domain-dkim \
  --domain "$DOMAIN" \
  --region "$REGION" \
  --query DkimTokens \
  --output json)

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  ADD THESE DNS RECORDS TO YOUR tqaentry.com NAMESERVERS"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "  SES domain verification (1 TXT record):"
printf "  %-8s  %-50s  %s\n" "TXT" "_amazonses.${DOMAIN}" "${VERIFY_TOKEN}"
echo ""
echo "  SES DKIM (3 CNAME records):"
while IFS= read -r TOKEN; do
  TOKEN=$(echo "$TOKEN" | tr -d '"')
  printf "  %-8s  %-65s  %s\n" "CNAME" "${TOKEN}._domainkey.${DOMAIN}" "${TOKEN}.dkim.amazonses.com"
done < <(echo "$DKIM_TOKENS" | jq -r '.[]')
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
read -r -p "Press Enter once you have added all four DNS records..."

# ── Poll SES verification ─────────────────────────────────────────────────────

log_info "Polling SES verification status (timeout: ${SES_POLL_TIMEOUT}s)..."
ELAPSED=0
VERIFY_STATUS=""
DKIM_STATUS=""

while (( ELAPSED < SES_POLL_TIMEOUT )); do
  VERIFY_STATUS=$(aws ses get-identity-verification-attributes \
    --identities "$DOMAIN" \
    --region "$REGION" \
    --query "VerificationAttributes.\"${DOMAIN}\".VerificationStatus" \
    --output text)
  DKIM_STATUS=$(aws ses get-identity-dkim-attributes \
    --identities "$DOMAIN" \
    --region "$REGION" \
    --query "DkimAttributes.\"${DOMAIN}\".DkimVerificationStatus" \
    --output text)

  log_info "SES domain: ${VERIFY_STATUS} | DKIM: ${DKIM_STATUS}"
  [[ "$VERIFY_STATUS" == "Success" && "$DKIM_STATUS" == "Success" ]] && break

  sleep 30
  (( ELAPSED += 30 ))
done

if [[ "$VERIFY_STATUS" != "Success" || "$DKIM_STATUS" != "Success" ]]; then
  log_warn "SES verification did not complete within the timeout."
  log_warn "Re-run this script later, or check manually:"
  log_warn "  aws ses get-identity-verification-attributes --identities ${DOMAIN} --region ${REGION}"
else
  log_info "SES domain verified successfully."
fi

log_info "Bootstrap complete. Run build.sh next."
