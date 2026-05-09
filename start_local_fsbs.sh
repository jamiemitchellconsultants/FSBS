#!/bin/bash

# --- Configuration ---
API_PORT=7177
WEB_PORT=7071
MAILPIT_WEB_PORT=8025
POSTGRES_PORT=5432

API_URL="https://localhost:${API_PORT}"
WEB_URL="https://localhost:${WEB_PORT}"
MAILPIT_URL="http://localhost:${MAILPIT_WEB_PORT}"

# --- Colors for better output ---
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# --- Helper functions ---

log_info() {
  echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
  echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
  echo -e "${RED}[ERROR]${NC} $1"
  exit 1
}

check_command() {
  if ! command -v "$1" &> /dev/null; then
    log_error "$1 is not installed. Please install it to proceed."
  fi
}

wait_for_url() {
  local url=$1
  local timeout=$2
  local interval=5
  log_info "Waiting for $url to be available (timeout: ${timeout}s)..."

  for ((i=0; i<timeout; i+=interval)); do
    local response
    local status_code

    # Capture body and status code so failed probes are visible in logs.
    response=$(curl -sS -k -w "\n%{http_code}" "$url")
    status_code=$(printf '%s\n' "$response" | tail -n 1)
    response=$(printf '%s\n' "$response" | sed '$d')

    log_info "curl status for $url: HTTP ${status_code}"
    if [[ -n "$response" ]]; then
      echo "$response"
    else
      log_info "curl response body is empty"
    fi

    if [[ "$status_code" -ge 200 && "$status_code" -lt 400 ]]; then
      log_info "$url is up!"
      return 0
    fi

    sleep "$interval"
  done

  log_error "$url did not become available within ${timeout}s."
}

# --- Main script logic ---

cleanup() {
  log_info "Stopping local FSBS infrastructure..."
  docker compose down
  log_info "Killing API and Web processes..."
  kill $(jobs -p) &> /dev/null
  log_info "Cleanup complete."
}

trap cleanup EXIT

log_info "Starting FSBS Local Development Environment Setup..."

# 1. Check prerequisites
log_info "Checking prerequisites..."
check_command "docker"
check_command "dotnet"
check_command "curl"
check_command "jq"

if ! dotnet tool list --global | grep -q "dotnet-ef"; then
  log_warn "dotnet-ef is not installed globally. Attempting to install..."
  dotnet tool install -g dotnet-ef || log_error "Failed to install dotnet-ef. Please install it manually: 'dotnet tool install -g dotnet-ef'"
fi
log_info "All prerequisites met."

# 2. Start local infrastructure (PostgreSQL, Redis, Mailpit)
log_info "Starting local Docker infrastructure (PostgreSQL, Redis, Mailpit)..."
docker compose up -d || log_error "Failed to start Docker Compose services."

# Wait for PostgreSQL to be healthy
log_info "Waiting for PostgreSQL to be healthy..."
for i in {1..10}; do # Max 10 attempts, 5 seconds each = 50 seconds
  PG_STATUS=$(docker compose ps postgres | grep postgres | awk '/\(healthy\)/ {print "(healthy)"}' ) #'{print $NF}'
  if [[ "$PG_STATUS" == "(healthy)" ]]; then
    log_info "PostgreSQL is healthy."
    break
  fi
  log_info "PostgreSQL status: $PG_STATUS. Waiting..."
  sleep 5
  if [[ $i -eq 10 ]]; then
    log_error "PostgreSQL did not become healthy in time."
  fi
done

log_info "Mailpit web interface available at: ${MAILPIT_URL}"

# 3. Apply database migrations
log_info "Applying database migrations..."
dotnet ef database update \
  -p src/FSBS.Infrastructure.Persistence.Migrations \
  -s src/FSBS.Infrastructure.Persistence.Migrations || log_error "Failed to apply database migrations."
log_info "Database migrations applied successfully."

# 4. Run the API
log_info "Starting FSBS API (logs to api.log)..."
ASPNETCORE_ENVIRONMENT=Development dotnet run --launch-profile "https" --project src/FSBS.Api > api.log 2>&1 &
API_PID=$!
log_info "API process ID: $API_PID"
wait_for_url "${API_URL}/health" 60 # Wait up to 60 seconds for API health endpoint

# 5. Run the Blazor frontend
log_info "Starting FSBS Web frontend (logs to web.log)..."
ASPNETCORE_ENVIRONMENT=Development dotnet run --launch-profile "https" --project src/FSBS.Web > web.log 2>&1 &
WEB_PID=$!
log_info "Web frontend process ID: $WEB_PID"
wait_for_url "${WEB_URL}" 60 # Wait up to 60 seconds for Web app to respond

log_info "FSBS system is up and running!"
log_info "You can access the Blazor frontend at: ${WEB_URL}"
log_info "You can access the API at: ${API_URL}"
log_info "Mailpit (email catcher) is at: ${MAILPIT_URL}"
log_info "API logs: tail -f api.log"
log_info "Web logs: tail -f web.log"

# 6. Seed demo data
log_info "Seeding demo data..."

# ── Fixed IDs ────────────────────────────────────────────────────────────────
ORG_ID="aaaaaaaa-0000-0000-0000-000000000001"
TENANT_ID="bbbbbbbb-0000-0000-0000-000000000001"
ACCOUNT_ID="cccccccc-0000-0000-0000-000000000001"
UNIT_ID="dddddddd-0000-0000-0000-000000000001"
BAY_A_ID="eeeeeeee-0000-0000-0000-000000000001"
BAY_B_ID="eeeeeeee-0000-0000-0000-000000000002"
CONFIG_FD_ID="ffffffff-0000-0000-0000-000000000001"
CONFIG_CC_ID="ffffffff-0000-0000-0000-000000000002"
POLICY_FD_STD_ID="11111111-0000-0000-0000-000000000001"
POLICY_FD_CORP_ID="11111111-0000-0000-0000-000000000002"
POLICY_FD_STAFF_ID="11111111-0000-0000-0000-000000000003"
POLICY_CC_STD_ID="11111111-0000-0000-0000-000000000004"
POLICY_CC_CORP_ID="11111111-0000-0000-0000-000000000005"
POLICY_CC_STAFF_ID="11111111-0000-0000-0000-000000000006"

PG_CONTAINER=$(docker compose ps -q postgres)

# ── Helper: run psql inside the container ────────────────────────────────────
run_sql() {
  docker exec "$PG_CONTAINER" psql -U postgres -d fsbs -c "$1"
}

# ── 1. Organisation + OrgAccount ─────────────────────────────────────────────
log_info "Seeding organisation 'Acme Airlines'..."
run_sql "
  INSERT INTO fsbs.organisations
    (org_id, tenant_id, name, customer_class, billing_email,
     credit_limit_gbp, is_active, is_deleted, created_at, updated_at)
  VALUES
    ('${ORG_ID}', '${TENANT_ID}', 'Acme Airlines', 'Corporate',
     'billing@acme-airlines.example', 50000, true, false, now(), now())
  ON CONFLICT (org_id) DO NOTHING;
" || log_warn "Organisation seed failed (may already exist)."

log_info "Seeding org account for Acme Airlines..."
run_sql "
  INSERT INTO fsbs.org_accounts
    (account_id, org_id, credit_limit_gbp, current_balance_gbp,
     status, payment_terms_days, created_at, updated_at)
  VALUES
    ('${ACCOUNT_ID}', '${ORG_ID}', 50000, 0,
     'Active', 30, now(), now())
  ON CONFLICT (org_id) DO NOTHING;
" || log_warn "OrgAccount seed failed (may already exist)."

# ── 2. Simulator unit, bays, and configurations ───────────────────────────────
# simulator_configurations must be inserted before simulator_units
# because simulator_units.active_configuration_id references simulator_configurations.
# simulator_configurations also requires simulator_unit_id, so we insert the unit
# first without an active_config, then insert configs, then update the unit.

log_info "Seeding simulator unit 'FFS-1'..."
run_sql "
  INSERT INTO fsbs.simulator_units
    (unit_id, name, fstd_level, manufacturer, location,
     default_reconfig_mins, is_active, is_deleted, created_at, updated_at)
  VALUES
    ('${UNIT_ID}', 'FFS-1', 'FFS Level D', 'CAE', 'Bay A, Building 1',
     60, true, false, now(), now())
  ON CONFLICT (unit_id) DO NOTHING;
" || log_warn "SimulatorUnit seed failed (may already exist)."

log_info "Seeding simulator bays..."
run_sql "
  INSERT INTO fsbs.simulator_bays
    (bay_id, simulator_unit_id, bay_code, status, is_deleted, created_at, updated_at)
  VALUES
    ('${BAY_A_ID}', '${UNIT_ID}', 'A', 'Operational', false, now(), now()),
    ('${BAY_B_ID}', '${UNIT_ID}', 'B', 'Operational', false, now(), now())
  ON CONFLICT (bay_id) DO NOTHING;
" || log_warn "SimulatorBay seed failed (may already exist)."

log_info "Seeding simulator configurations..."
run_sql "
  INSERT INTO fsbs.simulator_configurations
    (config_id, simulator_unit_id, name, aircraft_type, config_mode,
     supported_training_types, max_capacity_flight_deck, max_capacity_cabin_crew,
     is_active, is_deleted, created_at, updated_at)
  VALUES
    ('${CONFIG_FD_ID}', '${UNIT_ID}', 'B737-800 Flight Deck', 'B737-800',
     'CockpitOnly', ARRAY['flight_deck']::fsbs.training_type[],
     4, 10, true, false, now(), now()),
    ('${CONFIG_CC_ID}', '${UNIT_ID}', 'B737-800 Full Cabin', 'B737-800',
     'CockpitAndCabin', ARRAY['flight_deck','cabin_crew']::fsbs.training_type[],
     4, 10, true, false, now(), now())
  ON CONFLICT (config_id) DO NOTHING;
" || log_warn "SimulatorConfiguration seed failed (may already exist)."

log_info "Setting active configuration on FFS-1..."
run_sql "
  UPDATE fsbs.simulator_units
  SET active_configuration_id = '${CONFIG_FD_ID}', updated_at = now()
  WHERE unit_id = '${UNIT_ID}';
" || log_warn "SimulatorUnit active_config update failed."

# ── 3. Pricing policies ───────────────────────────────────────────────────────
log_info "Seeding pricing policies..."
run_sql "
  INSERT INTO fsbs.pricing_policies
    (policy_id, configuration_id, training_type, customer_class,
     hourly_rate_gbp, effective_from, is_deleted, created_at, updated_at)
  VALUES
    ('${POLICY_FD_STD_ID}',  '${CONFIG_FD_ID}', 'flight_deck', 'Standard',  120, '2025-01-01', false, now(), now()),
    ('${POLICY_FD_CORP_ID}', '${CONFIG_FD_ID}', 'flight_deck', 'Corporate', 110, '2025-01-01', false, now(), now()),
    ('${POLICY_FD_STAFF_ID}','${CONFIG_FD_ID}', 'flight_deck', 'Staff',      90, '2025-01-01', false, now(), now()),
    ('${POLICY_CC_STD_ID}',  '${CONFIG_CC_ID}', 'cabin_crew',  'Standard',  100, '2025-01-01', false, now(), now()),
    ('${POLICY_CC_CORP_ID}', '${CONFIG_CC_ID}', 'cabin_crew',  'Corporate',  90, '2025-01-01', false, now(), now()),
    ('${POLICY_CC_STAFF_ID}','${CONFIG_CC_ID}', 'cabin_crew',  'Staff',      75, '2025-01-01', false, now(), now())
  ON CONFLICT (policy_id) DO NOTHING;
" || log_warn "PricingPolicy seed failed (may already exist)."

# ── 4. Demo users (via API) ───────────────────────────────────────────────────
log_info "Seeding demo users via API..."

DEMO_STAFF_ROLES=("SystemAdmin" "ScheduleAdmin" "CourseDirector" "Instructor" "Management" "SalesStaff" "InternalStudent")
for ROLE in "${DEMO_STAFF_ROLES[@]}"; do
  EMAIL="${ROLE,,}@fsbs.local"
  log_info "  Seeding ${EMAIL} (${ROLE})..."
  curl -sf -X POST "${API_URL}/dev/users/seed?email=${EMAIL}&role=${ROLE}" -k > /dev/null \
    || log_warn "  Failed to seed ${EMAIL} (may already exist)."
done

# Private customer
curl -sf -X POST "${API_URL}/dev/users/seed?email=privatecustomer@fsbs.local&role=PrivateCustomer" -k > /dev/null \
  || log_warn "  Failed to seed privatecustomer@fsbs.local (may already exist)."

# Corporate users — seeded via API then linked to org via SQL
for ROLE in "CorporateManager" "CorporateStudent"; do
  EMAIL="${ROLE,,}@fsbs.local"
  log_info "  Seeding ${EMAIL} (${ROLE})..."
  USER_RESP=$(curl -sf -X POST "${API_URL}/dev/users/seed?email=${EMAIL}&role=${ROLE}" -k)
  if [[ $? -eq 0 ]]; then
    CORP_USER_ID=$(echo "$USER_RESP" | jq -r '.userId')
    if [[ -n "$CORP_USER_ID" && "$CORP_USER_ID" != "null" ]]; then
      ORG_ROLE="Manager"
      [[ "$ROLE" == "CorporateStudent" ]] && ORG_ROLE="Student"
      run_sql "
        INSERT INTO fsbs.org_memberships
          (membership_id, org_id, user_id, org_role, is_deleted, created_at, updated_at)
        VALUES
          (gen_random_uuid(), '${ORG_ID}', '${CORP_USER_ID}', '${ORG_ROLE}', false, now(), now())
        ON CONFLICT (org_id, user_id) DO NOTHING;
      " || log_warn "  OrgMembership insert failed for ${EMAIL}."
    fi
  else
    log_warn "  Failed to seed ${EMAIL} (may already exist)."
  fi
done

log_info "Demo data seeding complete."
log_info ""
log_info "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
log_info "Demo accounts (get a token with: curl -sk -X POST \"${API_URL}/dev/auth/token?email=<EMAIL>&role=<ROLE>\" | jq .)"
log_info ""
log_info "  systemadmin@fsbs.local       — SystemAdmin"
log_info "  scheduleadmin@fsbs.local     — ScheduleAdmin"
log_info "  coursedirector@fsbs.local    — CourseDirector"
log_info "  instructor@fsbs.local        — Instructor"
log_info "  management@fsbs.local        — Management"
log_info "  salesstaff@fsbs.local        — SalesStaff"
log_info "  internalstudent@fsbs.local   — InternalStudent"
log_info "  privatecustomer@fsbs.local   — PrivateCustomer"
log_info "  corporatemanager@fsbs.local  — CorporateManager (Acme Airlines)"
log_info "  corporatestudent@fsbs.local  — CorporateStudent (Acme Airlines)"
log_info "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

log_info "Setup complete. The system will continue running until you press Ctrl+C."
log_info "When you press Ctrl+C, all services and processes will be stopped."

# Keep the script running until interrupted
wait