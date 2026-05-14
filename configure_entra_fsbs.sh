#!/usr/bin/env bash
set -euo pipefail

APP_NAME="FSBS Staff Portal"
COGNITO_DOMAIN=""
TENANT_ID=""
SECRET_DISPLAY_NAME="FSBS Cognito Federation"
SECRET_YEARS=2
CREATE_SECRET=1
GRANT_ADMIN_CONSENT=1
CREATE_GROUPS=1
WRITE_AWS_SECRETS=0
DRY_RUN=0
ASSUME_YES=0
AWS_REGION=""
AWS_SECRET_PREFIX="fsbs/entra"
LOCAL_CALLBACK_URL="https://localhost:5001/auth/callback"
STAGING_CALLBACK_URL="https://staging.fsbs.tqaentry.com/auth/callback"
PROD_CALLBACK_URL="https://app.fsbs.tqaentry.com/auth/callback"
LOCAL_LOGOUT_URL="https://localhost:5001/logout"
STAGING_LOGOUT_URL="https://staging.fsbs.tqaentry.com/logout"
PROD_LOGOUT_URL="https://app.fsbs.tqaentry.com/logout"
APP_EXISTS=0
AZ_ACCOUNT_NAME=""
AZ_ACCOUNT_USER=""

ROLE_GROUPS=(
  "SystemAdmin"
  "ScheduleAdmin"
  "CourseDirector"
  "Management"
  "SalesStaff"
  "Instructor"
  "InternalStudent"
)

GRAPH_APP_ID="00000003-0000-0000-c000-000000000000"

GREEN='\033[0;32m'
YELLOW='\033[0;33m'
RED='\033[0;31m'
NC='\033[0m'

log_info() {
  echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
  echo -e "${YELLOW}[WARN]${NC} $1"
}

log_dry_run() {
  echo -e "${YELLOW}[DRY-RUN]${NC} $1"
}

log_error() {
  echo -e "${RED}[ERROR]${NC} $1" >&2
  exit 1
}

usage() {
  cat <<'EOF'
Usage:
  ./configure_entra_fsbs.sh --cognito-domain <domain> [options]

Required:
  --cognito-domain <domain>       Cognito hosted UI domain, with or without https://

Optional:
  --tenant-id <tenant-id>         Entra tenant ID. Defaults to the active az login tenant.
  --app-name <name>               App registration display name. Default: FSBS Staff Portal
  --secret-display-name <name>    Password credential display name. Default: FSBS Cognito Federation
  --secret-years <years>          Client secret lifetime in years. Default: 2
  --skip-secret                   Do not create/rotate a client secret
  --skip-admin-consent            Do not attempt to grant admin consent
  --skip-groups                   Do not create the seven FSBS staff security groups
  --write-aws-secrets             Write client-id, client-secret, and tenant-id to AWS Secrets Manager
  --dry-run                       Preview changes without mutating Entra or AWS
  --yes                           Skip the tenant confirmation prompt
  --aws-region <region>           AWS region for Secrets Manager writes
  --aws-secret-prefix <prefix>    Secrets Manager prefix. Default: fsbs/entra
  --local-callback-url <url>      Printed reminder for Cognito hosted UI config
  --staging-callback-url <url>    Printed reminder for Cognito hosted UI config
  --prod-callback-url <url>       Printed reminder for Cognito hosted UI config
  --local-logout-url <url>        Printed reminder for Cognito hosted UI config
  --staging-logout-url <url>      Printed reminder for Cognito hosted UI config
  --prod-logout-url <url>         Printed reminder for Cognito hosted UI config
  --help                          Show this help text

Examples:
  ./configure_entra_fsbs.sh \
    --cognito-domain fsbs-staff.auth.eu-west-1.amazoncognito.com

  ./configure_entra_fsbs.sh \
    --cognito-domain https://fsbs-staff.auth.eu-west-1.amazoncognito.com \
    --write-aws-secrets \
    --aws-region eu-west-1

  ./configure_entra_fsbs.sh \
    --cognito-domain fsbs-staff.auth.eu-west-1.amazoncognito.com \
    --dry-run
EOF
}

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    log_error "Required command '$1' is not installed or not on PATH."
  fi
}

validate_positive_integer() {
  local value="$1"
  local field_name="$2"

  if ! [[ "$value" =~ ^[1-9][0-9]*$ ]]; then
    log_error "${field_name} must be a positive integer."
  fi
}

slugify() {
  printf '%s' "$1" \
    | tr '[:upper:]' '[:lower:]' \
    | sed -E 's/[^a-z0-9]+/-/g; s/^-+//; s/-+$//'
}

normalize_cognito_domain() {
  local domain="$1"
  domain="${domain#https://}"
  domain="${domain#http://}"
  domain="${domain%/}"
  printf '%s' "$domain"
}

parse_args() {
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --cognito-domain)
        COGNITO_DOMAIN="$2"
        shift 2
        ;;
      --tenant-id)
        TENANT_ID="$2"
        shift 2
        ;;
      --app-name)
        APP_NAME="$2"
        shift 2
        ;;
      --secret-display-name)
        SECRET_DISPLAY_NAME="$2"
        shift 2
        ;;
      --secret-years)
        SECRET_YEARS="$2"
        shift 2
        ;;
      --skip-secret)
        CREATE_SECRET=0
        shift
        ;;
      --skip-admin-consent)
        GRANT_ADMIN_CONSENT=0
        shift
        ;;
      --skip-groups)
        CREATE_GROUPS=0
        shift
        ;;
      --write-aws-secrets)
        WRITE_AWS_SECRETS=1
        shift
        ;;
      --dry-run)
        DRY_RUN=1
        shift
        ;;
      --yes)
        ASSUME_YES=1
        shift
        ;;
      --aws-region)
        AWS_REGION="$2"
        shift 2
        ;;
      --aws-secret-prefix)
        AWS_SECRET_PREFIX="$2"
        shift 2
        ;;
      --local-callback-url)
        LOCAL_CALLBACK_URL="$2"
        shift 2
        ;;
      --staging-callback-url)
        STAGING_CALLBACK_URL="$2"
        shift 2
        ;;
      --prod-callback-url)
        PROD_CALLBACK_URL="$2"
        shift 2
        ;;
      --local-logout-url)
        LOCAL_LOGOUT_URL="$2"
        shift 2
        ;;
      --staging-logout-url)
        STAGING_LOGOUT_URL="$2"
        shift 2
        ;;
      --prod-logout-url)
        PROD_LOGOUT_URL="$2"
        shift 2
        ;;
      --help|-h)
        usage
        exit 0
        ;;
      *)
        log_error "Unknown argument: $1"
        ;;
    esac
  done

  if [[ -z "$COGNITO_DOMAIN" ]]; then
    usage
    log_error "--cognito-domain is required."
  fi

  if [[ "$WRITE_AWS_SECRETS" -eq 1 && -z "$AWS_REGION" ]]; then
    log_error "--aws-region is required when --write-aws-secrets is used."
  fi

  if [[ "$WRITE_AWS_SECRETS" -eq 1 && "$CREATE_SECRET" -ne 1 ]]; then
    log_error "--write-aws-secrets requires a newly generated secret. Remove --skip-secret or disable --write-aws-secrets."
  fi

  validate_positive_integer "$SECRET_YEARS" "--secret-years"
}

ensure_az_login() {
  if ! az account show >/dev/null 2>&1; then
    log_error "Azure CLI is not logged in. Run 'az login' first."
  fi
}

resolve_tenant_id() {
  if [[ -z "$TENANT_ID" ]]; then
    TENANT_ID=$(az account show --query tenantId -o tsv)
  fi

  if [[ -z "$TENANT_ID" ]]; then
    log_error "Unable to resolve tenant ID from Azure CLI context. Pass --tenant-id explicitly."
  fi
}

fetch_account_context() {
  local account_json
  account_json=$(az account show -o json)

  AZ_ACCOUNT_NAME=$(printf '%s' "$account_json" | jq -r '.name // empty')
  AZ_ACCOUNT_USER=$(printf '%s' "$account_json" | jq -r '.user.name // empty')
}

confirm_execution_context() {
  if [[ "$DRY_RUN" -eq 1 ]]; then
    log_dry_run "Skipping confirmation prompt because no changes will be applied."
    return
  fi

  if [[ "$ASSUME_YES" -eq 1 ]]; then
    log_warn "Skipping tenant confirmation prompt because --yes was provided."
    return
  fi

  if [[ ! -t 0 ]]; then
    log_error "Refusing to modify tenant ${TENANT_ID} without an interactive confirmation prompt. Re-run with --yes if this is intentional."
  fi

  cat <<EOF

About to make live changes in Microsoft Entra ID.

Tenant ID:       ${TENANT_ID}
Azure account:   ${AZ_ACCOUNT_NAME:-<unknown>}
Signed-in user:  ${AZ_ACCOUNT_USER:-<unknown>}
App name:        ${APP_NAME}
Cognito domain:  ${COGNITO_DOMAIN}
Create secret:   $([[ "$CREATE_SECRET" -eq 1 ]] && echo yes || echo no)
Create groups:   $([[ "$CREATE_GROUPS" -eq 1 ]] && echo yes || echo no)
Admin consent:   $([[ "$GRANT_ADMIN_CONSENT" -eq 1 ]] && echo yes || echo no)
Write AWS secrets: $([[ "$WRITE_AWS_SECRETS" -eq 1 ]] && echo yes || echo no)

EOF

  local confirmation
  read -r -p "Type the tenant ID (${TENANT_ID}) to continue: " confirmation
  if [[ "$confirmation" != "$TENANT_ID" ]]; then
    log_error "Confirmation failed. No Entra changes were applied."
  fi
}

get_graph_permission_id() {
  local source_json="$1"
  local query_type="$2"
  local value="$3"

  case "$query_type" in
    scope)
      printf '%s' "$source_json" | jq -r --arg value "$value" '.oauth2PermissionScopes[] | select(.value == $value) | .id' | head -n 1
      ;;
    role)
      printf '%s' "$source_json" | jq -r --arg value "$value" '.appRoles[] | select(.value == $value) | .id' | head -n 1
      ;;
    *)
      log_error "Unsupported permission query type: $query_type"
      ;;
  esac
}

fetch_graph_permission_ids() {
  log_info "Resolving Microsoft Graph permission IDs..."
  local graph_sp_json
  graph_sp_json=$(az ad sp show --id "$GRAPH_APP_ID" -o json)

  OPENID_SCOPE_ID=$(get_graph_permission_id "$graph_sp_json" scope "openid")
  EMAIL_SCOPE_ID=$(get_graph_permission_id "$graph_sp_json" scope "email")
  PROFILE_SCOPE_ID=$(get_graph_permission_id "$graph_sp_json" scope "profile")
  GROUP_MEMBER_READ_ALL_ROLE_ID=$(get_graph_permission_id "$graph_sp_json" role "GroupMember.Read.All")
  USER_READ_ALL_ROLE_ID=$(get_graph_permission_id "$graph_sp_json" role "User.Read.All")

  for permission_id in \
    "$OPENID_SCOPE_ID" \
    "$EMAIL_SCOPE_ID" \
    "$PROFILE_SCOPE_ID" \
    "$GROUP_MEMBER_READ_ALL_ROLE_ID" \
    "$USER_READ_ALL_ROLE_ID"; do
    if [[ -z "$permission_id" || "$permission_id" == "null" ]]; then
      log_error "Failed to resolve one or more Microsoft Graph permission IDs."
    fi
  done
}

select_or_create_app() {
  log_info "Looking for existing Entra app registration named '${APP_NAME}'..."

  local matches_json
  matches_json=$(az ad app list --display-name "$APP_NAME" -o json)

  local match_count
  match_count=$(printf '%s' "$matches_json" | jq 'length')

  if [[ "$match_count" -eq 0 ]]; then
    APP_EXISTS=0

    if [[ "$DRY_RUN" -eq 1 ]]; then
      APP_OBJECT_ID="<dry-run-new-app-object-id>"
      APP_CLIENT_ID="<dry-run-new-app-client-id>"
      log_dry_run "Would create Entra app registration '${APP_NAME}' with redirect URI ${ENTRA_REDIRECT_URI}."
      return
    fi

    log_info "No existing app registration found. Creating '${APP_NAME}'..."
    local created_json
    created_json=$(az ad app create \
      --display-name "$APP_NAME" \
      --sign-in-audience AzureADMyOrg \
      --web-redirect-uris "$ENTRA_REDIRECT_URI" \
      -o json)

    APP_OBJECT_ID=$(printf '%s' "$created_json" | jq -r '.id')
    APP_CLIENT_ID=$(printf '%s' "$created_json" | jq -r '.appId')
    return
  fi

  APP_EXISTS=1
  if [[ "$match_count" -gt 1 ]]; then
    log_warn "Multiple app registrations matched '${APP_NAME}'. Reusing the first result."
  fi

  APP_OBJECT_ID=$(printf '%s' "$matches_json" | jq -r '.[0].id')
  APP_CLIENT_ID=$(printf '%s' "$matches_json" | jq -r '.[0].appId')
  log_info "Reusing existing app registration with client ID ${APP_CLIENT_ID}."
}

fetch_app_json() {
  az ad app show --id "$APP_CLIENT_ID" -o json
}

build_patch_body() {
  local current_json="$1"

  jq -cn \
    --argjson current "$current_json" \
    --arg redirectUri "$ENTRA_REDIRECT_URI" \
    --arg graphAppId "$GRAPH_APP_ID" \
    --arg openidScopeId "$OPENID_SCOPE_ID" \
    --arg emailScopeId "$EMAIL_SCOPE_ID" \
    --arg profileScopeId "$PROFILE_SCOPE_ID" \
    --arg groupMemberReadAllRoleId "$GROUP_MEMBER_READ_ALL_ROLE_ID" \
    --arg userReadAllRoleId "$USER_READ_ALL_ROLE_ID" \
    '
    def requiredClaims:
      [
        {name: "email", essential: false, additionalProperties: []},
        {name: "given_name", essential: false, additionalProperties: []},
        {name: "family_name", essential: false, additionalProperties: []},
        {name: "groups", essential: false, additionalProperties: ["cloud_displayname"]}
      ];

    def graphPermissions:
      [
        {id: $openidScopeId, type: "Scope"},
        {id: $emailScopeId, type: "Scope"},
        {id: $profileScopeId, type: "Scope"},
        {id: $groupMemberReadAllRoleId, type: "Role"},
        {id: $userReadAllRoleId, type: "Role"}
      ];

    def existingGraphPermissions:
      ((($current.requiredResourceAccess // [])
        | map(select(.resourceAppId == $graphAppId))
        | .[0].resourceAccess) // []);

    {
      signInAudience: "AzureADMyOrg",
      web: {
        redirectUris: (((($current.web.redirectUris // []) + [$redirectUri]) | unique))
      },
      groupMembershipClaims: "SecurityGroup",
      optionalClaims: {
        idToken: (
          (($current.optionalClaims.idToken // [])
            | map(select(.name != "email" and .name != "given_name" and .name != "family_name" and .name != "groups")))
          + requiredClaims
        )
      },
      requiredResourceAccess: (
        (($current.requiredResourceAccess // []) | map(select(.resourceAppId != $graphAppId)))
        + [
            {
              resourceAppId: $graphAppId,
              resourceAccess: ((existingGraphPermissions + graphPermissions) | unique_by(.id + "|" + .type))
            }
          ]
      )
    }
    '
}

configure_app_manifest() {
  log_info "Applying Entra app manifest updates (redirect URI, token claims, Graph permissions)..."
  local current_json
  if [[ "$DRY_RUN" -eq 1 && "$APP_EXISTS" -eq 0 ]]; then
    current_json='{}'
  else
    current_json=$(fetch_app_json)
  fi

  local patch_body
  patch_body=$(build_patch_body "$current_json")

  if [[ "$DRY_RUN" -eq 1 ]]; then
    log_dry_run "Would patch the Entra app manifest for '${APP_NAME}'."
    return
  fi

  az rest \
    --method PATCH \
    --uri "https://graph.microsoft.com/v1.0/applications/${APP_OBJECT_ID}" \
    --headers 'Content-Type=application/json' \
    --body "$patch_body" >/dev/null
}

ensure_service_principal() {
  if [[ "$DRY_RUN" -eq 1 && "$APP_EXISTS" -eq 0 ]]; then
    log_dry_run "Would create the enterprise application for '${APP_NAME}' after app registration creation."
    return
  fi

  if az ad sp show --id "$APP_CLIENT_ID" >/dev/null 2>&1; then
    log_info "Enterprise application already exists for client ID ${APP_CLIENT_ID}."
    return
  fi

  if [[ "$DRY_RUN" -eq 1 ]]; then
    log_dry_run "Would create the enterprise application for client ID ${APP_CLIENT_ID}."
    return
  fi

  log_info "Creating enterprise application for client ID ${APP_CLIENT_ID}..."
  az ad sp create --id "$APP_CLIENT_ID" >/dev/null
}

create_client_secret() {
  if [[ "$CREATE_SECRET" -ne 1 ]]; then
    log_warn "Skipping client secret creation as requested."
    CLIENT_SECRET_VALUE=""
    return
  fi

  if [[ "$DRY_RUN" -eq 1 ]]; then
    CLIENT_SECRET_VALUE="<dry-run-secret-not-generated>"
    log_dry_run "Would create a new client secret '${SECRET_DISPLAY_NAME}' valid for ${SECRET_YEARS} year(s)."
    return
  fi

  log_info "Creating a new client secret '${SECRET_DISPLAY_NAME}' valid for ${SECRET_YEARS} year(s)..."
  CLIENT_SECRET_VALUE=$(az ad app credential reset \
    --id "$APP_CLIENT_ID" \
    --append \
    --display-name "$SECRET_DISPLAY_NAME" \
    --years "$SECRET_YEARS" \
    --query password \
    -o tsv)

  if [[ -z "$CLIENT_SECRET_VALUE" ]]; then
    log_error "Azure CLI did not return a client secret value."
  fi
}

grant_admin_consent() {
  if [[ "$GRANT_ADMIN_CONSENT" -ne 1 ]]; then
    log_warn "Skipping admin consent as requested."
    return
  fi

  if [[ "$DRY_RUN" -eq 1 ]]; then
    log_dry_run "Would grant admin consent for Microsoft Graph permissions to app ID ${APP_CLIENT_ID}."
    return
  fi

  log_info "Attempting to grant admin consent for Microsoft Graph permissions..."
  if ! az ad app permission admin-consent --id "$APP_CLIENT_ID" >/dev/null 2>&1; then
    log_warn "Admin consent could not be granted automatically. Run this manually with an admin account: az ad app permission admin-consent --id ${APP_CLIENT_ID}"
  else
    log_info "Admin consent granted successfully."
  fi
}

ensure_group() {
  local group_name="$1"
  local existing_id
  existing_id=$(az ad group list --filter "displayName eq '${group_name}'" --query '[0].id' -o tsv)

  if [[ -n "$existing_id" ]]; then
    log_info "Security group '${group_name}' already exists."
    return
  fi

  local mail_nickname="fsbs-$(slugify "$group_name")"

  if [[ "$DRY_RUN" -eq 1 ]]; then
    log_dry_run "Would create security group '${group_name}' with mail nickname '${mail_nickname}'."
    return
  fi

  log_info "Creating security group '${group_name}'..."
  az ad group create \
    --display-name "$group_name" \
    --mail-nickname "$mail_nickname" \
    --security-enabled true \
    --mail-enabled false >/dev/null
}

ensure_role_groups() {
  if [[ "$CREATE_GROUPS" -ne 1 ]]; then
    log_warn "Skipping security group creation as requested."
    return
  fi

  log_info "Ensuring the seven FSBS staff role groups exist..."
  local role
  for role in "${ROLE_GROUPS[@]}"; do
    ensure_group "$role"
  done
}

put_aws_secret() {
  local secret_name="$1"
  local secret_value="$2"

  if aws secretsmanager describe-secret --region "$AWS_REGION" --secret-id "$secret_name" >/dev/null 2>&1; then
    aws secretsmanager put-secret-value \
      --region "$AWS_REGION" \
      --secret-id "$secret_name" \
      --secret-string "$secret_value" >/dev/null
  else
    aws secretsmanager create-secret \
      --region "$AWS_REGION" \
      --name "$secret_name" \
      --secret-string "$secret_value" >/dev/null
  fi
}

write_aws_secrets() {
  if [[ "$WRITE_AWS_SECRETS" -ne 1 ]]; then
    return
  fi

  if [[ -z "$CLIENT_SECRET_VALUE" ]]; then
    log_error "Cannot write AWS secrets because no client secret value is available. Omit --skip-secret or disable --write-aws-secrets."
  fi

  if [[ "$DRY_RUN" -eq 1 ]]; then
    log_dry_run "Would write the Entra client ID, client secret, and tenant ID to AWS Secrets Manager in ${AWS_REGION}."
    return
  fi

  require_command aws

  log_info "Writing Entra credentials to AWS Secrets Manager in ${AWS_REGION}..."
  put_aws_secret "${AWS_SECRET_PREFIX}/client-id" "$APP_CLIENT_ID"
  put_aws_secret "${AWS_SECRET_PREFIX}/client-secret" "$CLIENT_SECRET_VALUE"
  put_aws_secret "${AWS_SECRET_PREFIX}/tenant-id" "$TENANT_ID"
}

print_summary() {
  cat <<EOF

Configured Entra application successfully.

Dry-run mode:            $([[ "$DRY_RUN" -eq 1 ]] && echo yes || echo no)
Application name:        ${APP_NAME}
Application (client) ID: ${APP_CLIENT_ID}
Application object ID:   ${APP_OBJECT_ID}
Tenant ID:               ${TENANT_ID}
Issuer URL:              https://login.microsoftonline.com/${TENANT_ID}/v2.0
Entra redirect URI:      ${ENTRA_REDIRECT_URI}

Configured in the app registration:
  - Single-tenant sign-in audience
  - Redirect URI for Cognito /oauth2/idpresponse
  - ID token optional claims: email, given_name, family_name
  - Groups claim emitted with cloud display names
  - Microsoft Graph delegated permissions: openid, email, profile
  - Microsoft Graph application permissions: GroupMember.Read.All, User.Read.All
  - Seven FSBS staff security groups
EOF

  if [[ -n "$CLIENT_SECRET_VALUE" ]]; then
    cat <<EOF

New client secret created.
Store this immediately in AWS Secrets Manager or another secure vault:
  Secret display name: ${SECRET_DISPLAY_NAME}
  Secret value: ${CLIENT_SECRET_VALUE}
EOF
  elif [[ "$DRY_RUN" -eq 1 && "$CREATE_SECRET" -eq 1 ]]; then
    cat <<EOF

Dry-run only: a client secret was not actually generated.
If you run without --dry-run, a new secret will be created with display name:
  ${SECRET_DISPLAY_NAME}
EOF
  else
    cat <<EOF

No new client secret was created by this run.
EOF
  fi

  cat <<EOF

Still required outside Entra:
  1. Configure Cognito Staff Pool OIDC federation with:
     - Client ID: ${APP_CLIENT_ID}
     - Client secret: <the value above>
     - Issuer URL: https://login.microsoftonline.com/${TENANT_ID}/v2.0
  2. In Cognito hosted UI, set callback URLs:
     - ${LOCAL_CALLBACK_URL}
     - ${STAGING_CALLBACK_URL}
     - ${PROD_CALLBACK_URL}
  3. In Cognito hosted UI, set sign-out URLs:
     - ${LOCAL_LOGOUT_URL}
     - ${STAGING_LOGOUT_URL}
     - ${PROD_LOGOUT_URL}
  4. Assign staff users to one or more of these Entra groups:
     - ${ROLE_GROUPS[*]}

If you enabled AWS secret writes, these secrets were updated:
  - ${AWS_SECRET_PREFIX}/client-id
  - ${AWS_SECRET_PREFIX}/client-secret
  - ${AWS_SECRET_PREFIX}/tenant-id
EOF
}

main() {
  parse_args "$@"

  require_command az
  require_command jq
  ensure_az_login

  COGNITO_DOMAIN=$(normalize_cognito_domain "$COGNITO_DOMAIN")
  ENTRA_REDIRECT_URI="https://${COGNITO_DOMAIN}/oauth2/idpresponse"

  resolve_tenant_id
  fetch_account_context
  confirm_execution_context
  fetch_graph_permission_ids
  select_or_create_app
  configure_app_manifest
  ensure_service_principal
  create_client_secret
  ensure_role_groups
  grant_admin_consent
  write_aws_secrets
  print_summary
}

main "$@"



