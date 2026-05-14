# Entra configuration automation script

`configure_entra_fsbs.sh` automates the Entra-side steps from `DevNotes/EntraConfiguration.md`.

## What it does

The script creates or updates the `FSBS Staff Portal` app registration and applies the configuration required for the staff Cognito federation flow:

- creates or reuses the Entra app registration
- sets the Cognito `oauth2/idpresponse` redirect URI
- configures ID token claims for `email`, `given_name`, and `family_name`
- configures the groups claim to emit cloud group display names
- adds Microsoft Graph delegated permissions: `openid`, `email`, `profile`
- adds Microsoft Graph application permissions: `GroupMember.Read.All`, `User.Read.All`
- creates the seven FSBS staff Entra security groups if they do not already exist
- optionally creates a client secret
- optionally writes `client-id`, `client-secret`, and `tenant-id` to AWS Secrets Manager
- supports a safe `--dry-run` preview mode
- requires an explicit tenant confirmation prompt before live changes, unless `--yes` is provided

## What it does not do

The script does **not** configure Cognito for you. After it completes, you still need to:

1. add the Entra OIDC identity provider in the Cognito staff pool
2. configure the Cognito hosted UI callback and sign-out URLs:
   - `https://localhost:5001/auth/callback`
   - `https://staging.fsbs.tqaentry.com/auth/callback`
   - `https://app.fsbs.tqaentry.com/auth/callback`
3. populate `FSBS.Web/wwwroot/appsettings.json` and API Cognito settings
4. assign real staff users to the Entra security groups

## Prerequisites

- Azure CLI (`az`) installed and logged in
- `jq` installed
- AWS CLI installed only if you plan to use `--write-aws-secrets`
- an Entra admin account if you want the script to grant admin consent automatically

## Example usage

```bash
./configure_entra_fsbs.sh \
  --cognito-domain fsbs-staff.auth.eu-west-1.amazoncognito.com
```

Preview changes without mutating Entra or AWS:

```bash
./configure_entra_fsbs.sh \
  --cognito-domain fsbs-staff.auth.eu-west-1.amazoncognito.com \
  --dry-run
```

With AWS Secrets Manager writes enabled:

```bash
./configure_entra_fsbs.sh \
  --cognito-domain fsbs-staff.auth.eu-west-1.amazoncognito.com \
  --write-aws-secrets \
  --aws-region eu-west-1
```

Skip client secret rotation if you are only reconciling the manifest and groups:

```bash
./configure_entra_fsbs.sh \
  --cognito-domain fsbs-staff.auth.eu-west-1.amazoncognito.com \
  --skip-secret
```

Skip the interactive tenant confirmation prompt for automation scenarios:

```bash
./configure_entra_fsbs.sh \
  --cognito-domain fsbs-staff.auth.eu-west-1.amazoncognito.com \
  --yes
```

## Safety features

- live runs prompt you to type the target tenant ID before any Entra mutations are applied
- `--yes` suppresses that prompt for CI or other controlled automation
- `--dry-run` performs a read-only preview and does not mutate Entra or AWS Secrets Manager
- the script rejects `--write-aws-secrets` combined with `--skip-secret`, because there would be no fresh secret value to store

## Outputs

The script prints:

- the Entra application client ID
- the Entra application object ID
- the tenant ID
- the issuer URL to use in Cognito
- the Cognito redirect URI registered in Entra
- the newly created client secret value, if one was generated

## Verification

A minimal local validation pass for the script itself is:

```bash
bash -n ./configure_entra_fsbs.sh
./configure_entra_fsbs.sh --help
./configure_entra_fsbs.sh --cognito-domain fsbs-staff.auth.eu-west-1.amazoncognito.com --dry-run
```

For the full environment validation checklist, continue with `DevNotes/EntraConfiguration.md`.


