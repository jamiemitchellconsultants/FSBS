#!/usr/bin/env bash
set -euo pipefail

REGION="eu-west-1"
S3_BUCKET="fsbs-static-679777944071"
APP_URL="https://staging.fsbs.tqaentry.com"
APPSETTINGS="src/FSBS.Web/wwwroot/appsettings.json"
OUTPUTS_FILE="deploy-outputs.env"

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
require_command dotnet
require_command jq

# ── Load stack outputs ────────────────────────────────────────────────────────

[[ -f "$OUTPUTS_FILE" ]] \
  || log_error "${OUTPUTS_FILE} not found. Run deploy.sh first, or create it manually with the CDK stack output values."

# shellcheck source=/dev/null
source "$OUTPUTS_FILE"

: "${STAFF_CLIENT_ID:?Missing STAFF_CLIENT_ID in ${OUTPUTS_FILE}}"
: "${COGNITO_DOMAIN_PREFIX:?Missing COGNITO_DOMAIN_PREFIX in ${OUTPUTS_FILE}}"
: "${CDN_DOMAIN:?Missing CDN_DOMAIN in ${OUTPUTS_FILE}}"

# ── Patch appsettings.json ────────────────────────────────────────────────────

log_info "Writing ${APPSETTINGS}..."
# Use /oauth2/authorize directly with identity_provider=EntraID to bypass the
# Cognito hosted UI. The hosted UI's EntraID button generates an unencoded
# redirect_uri in its onclick URL, which Cognito's /oauth2/authorize endpoint
# rejects with 400. Going directly to /oauth2/authorize with properly encoded
# params skips the hosted UI entirely.
STAFF_LOGIN_URL="https://${COGNITO_DOMAIN_PREFIX}.auth.${REGION}.amazoncognito.com/oauth2/authorize"
STAFF_LOGIN_URL+="?identity_provider=EntraID"
STAFF_LOGIN_URL+="&client_id=${STAFF_CLIENT_ID}"
STAFF_LOGIN_URL+="&response_type=code"
STAFF_LOGIN_URL+="&scope=openid+email+profile"
# Cognito posts the code to the API callback (/v1/auth/callback), not the SPA.
# The API uses state=staff|... to route token exchange to the staff pool.
STAFF_LOGIN_URL+="&redirect_uri=https%3A%2F%2Fstaging.fsbs.tqaentry.com%2Fv1%2Fauth%2Fcallback"
STAFF_LOGIN_URL+="&state=staff%7Cfrontend"

cat > "$APPSETTINGS" <<EOF
{
  "ApiBaseUrl": "${APP_URL}",
  "Cognito": {
    "StaffPoolLoginUrl": "${STAFF_LOGIN_URL}"
  }
}
EOF

# ── Rebuild Blazor WASM ───────────────────────────────────────────────────────

log_info "Rebuilding Blazor WASM with updated appsettings..."
dotnet publish src/FSBS.Web/FSBS.Web.csproj \
  -c Release \
  -o publish/web

# ── Upload to S3 ──────────────────────────────────────────────────────────────

log_info "Syncing to s3://${S3_BUCKET}..."
aws s3 sync publish/web/wwwroot "s3://${S3_BUCKET}" --delete

# ── Invalidate CloudFront ─────────────────────────────────────────────────────

log_info "Finding CloudFront distribution for staging.fsbs.tqaentry.com..."
DIST_ID=$(aws cloudfront list-distributions \
  --query "DistributionList.Items[?contains(Aliases.Items, 'staging.fsbs.tqaentry.com')].Id" \
  --output text)
[[ -n "$DIST_ID" ]] || log_error "No CloudFront distribution found with alias 'staging.fsbs.tqaentry.com'."

log_info "Creating CloudFront invalidation (distribution ${DIST_ID})..."
aws cloudfront create-invalidation \
  --distribution-id "$DIST_ID" \
  --paths "/*" \
  --query "Invalidation.Id" \
  --output text

# ── Health check ─────────────────────────────────────────────────────────────

log_info "Waiting 15 seconds for invalidation to propagate..."
sleep 15

log_info "Health check: ${APP_URL}/v1/health"
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "${APP_URL}/v1/health" || true)
if [[ "$HTTP_STATUS" == "200" ]]; then
  log_info "API is healthy (HTTP 200)."
else
  log_warn "Health check returned HTTP ${HTTP_STATUS}. The app may still be starting up."
  log_warn "Navigate to ${APP_URL} and verify manually."
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  Frontend deployed to ${APP_URL}"
echo "  Navigate there and sign in with a staff Entra account to smoke test."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
