#!/usr/bin/env bash
set -euo pipefail

ACCOUNT_ID="679777944071"
REGION="eu-west-1"
ECR_BASE="${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com"

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
require_command docker
require_command dotnet

docker info >/dev/null 2>&1 || log_error "Docker daemon is not running. Start Docker Desktop and try again."

# ── ECR login ─────────────────────────────────────────────────────────────────

log_info "Authenticating Docker with ECR..."
aws ecr get-login-password --region "$REGION" \
  | docker login --username AWS --password-stdin "${ECR_BASE}"

# ── API image ─────────────────────────────────────────────────────────────────

log_info "Building fsbs-api (linux/amd64)..."
docker build --platform linux/amd64 \
  -t fsbs-api:latest \
  -f src/FSBS.Api/Dockerfile .

log_info "Tagging and pushing fsbs-api:latest..."
docker tag fsbs-api:latest "${ECR_BASE}/fsbs-api:latest"
docker push "${ECR_BASE}/fsbs-api:latest"

# ── Worker image ──────────────────────────────────────────────────────────────

log_info "Building fsbs-worker (linux/amd64)..."
docker build --platform linux/amd64 \
  -t fsbs-worker:latest \
  -f src/FSBS.Worker/Dockerfile .

log_info "Tagging and pushing fsbs-worker:latest..."
docker tag fsbs-worker:latest "${ECR_BASE}/fsbs-worker:latest"
docker push "${ECR_BASE}/fsbs-worker:latest"

# ── Lambda functions ──────────────────────────────────────────────────────────

log_info "Publishing Lambda functions..."
dotnet publish src/FSBS.Functions/FSBS.Functions.csproj \
  -c Release \
  -o infrastructure/FSBS.Cdk/.artifacts/functions

# ── Blazor WASM ───────────────────────────────────────────────────────────────

log_info "Building Blazor WASM frontend..."
dotnet publish src/FSBS.Web/FSBS.Web.csproj \
  -c Release \
  -o publish/web

log_info ""
log_info "Build complete."
log_info "  API image:     ${ECR_BASE}/fsbs-api:latest"
log_info "  Worker image:  ${ECR_BASE}/fsbs-worker:latest"
log_info "  Lambdas:       infrastructure/FSBS.Cdk/.artifacts/functions/"
log_info "  Blazor WASM:   publish/web/"
log_info ""
log_info "Run deploy.sh next."
