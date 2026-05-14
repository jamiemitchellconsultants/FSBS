# Entra ID Configuration for FSBS

This document covers the Microsoft Entra ID (Azure AD) app registration required
for the FSBS staff authentication flow.

> **Localhost note:** When `DevAuth:Enabled = true` (the default in
> `appsettings.Development.json`) the entire Cognito + Entra stack is bypassed.
> Use `POST /dev/auth/token?role=SystemAdmin` to get a signed JWT for any role.
> The configuration below is required for **staging / UAT / production**, or if
> you want to test the real Entra → Cognito federation flow locally.

> **Automation note:** You can automate the Entra-side setup with
> `./configure_entra_fsbs.sh`, including a safe `--dry-run` preview mode.
> A quick usage guide lives in `DevNotes/EntraConfigurationScript.md`.

---

## Overview

The staff authentication chain is:

```
Browser
  → Cognito Staff Pool hosted UI
  → Microsoft Entra ID (OIDC federation)
  → Cognito Post-Confirmation Lambda fires on first sign-in
      → reads custom:entra_groups attribute
      → maps to FSBS app_role
      → upserts fsbs.app_users row
      → calls AdminAddUserToGroup on Cognito
  → Cognito issues JWT
  → API validates JWT → /availability
```

---

## Step 1 — Register the application in Entra ID

1. Go to **Azure Portal → Microsoft Entra ID → App registrations → New registration**
2. Configure:

| Field | Value |
|---|---|
| Name | `FSBS Staff Portal` |
| Supported account types | **Accounts in this organizational directory only** (single tenant) |
| Redirect URI — platform | **Web** |
| Redirect URI — value | `https://<cognito-domain>.auth.eu-west-1.amazoncognito.com/oauth2/idpresponse` |

For localhost testing against a real Cognito pool, add a second redirect URI pointing
to your Cognito domain — the redirect goes to Cognito, not directly to localhost.

3. Click **Register**. Note the **Application (client) ID** and **Directory (tenant) ID**.

---

## Step 2 — Create a client secret

1. App registration → **Certificates & secrets → New client secret**
2. Description: `FSBS Cognito Federation`
3. Expiry: 24 months
4. Copy the **Value** immediately — store it in AWS Secrets Manager as `fsbs/entra/client-secret`

---

## Step 3 — Configure token claims

### 3a — Optional claims (ID token)

Go to **Token configuration → Add optional claim → Token type: ID**:

| Claim | Purpose |
|---|---|
| `email` | User's email address |
| `given_name` | First name |
| `family_name` | Last name |

When prompted, enable **Turn on the Microsoft Graph email permission** — click Yes.

### 3b — Groups claim

Go to **Token configuration → Add groups claim**:

- Select **Security groups**
- Under **ID token**: choose **Group display name** (requires `profile` scope)

> Using display names means the Cognito attribute `custom:entra_groups` will contain
> the group names directly (e.g. `SystemAdmin,Instructor`) which the Post-Confirmation
> Lambda matches against without any ID-to-name lookup.

---

## Step 4 — Create Entra security groups

Create one security group per FSBS staff role. The **display name must exactly match**
the role string (case-insensitive) that the Post-Confirmation Lambda expects:

| Group display name | FSBS role assigned | Typical members |
|---|---|---|
| `SystemAdmin` | `SystemAdmin` | IT administrators, senior ops |
| `ScheduleAdmin` | `ScheduleAdmin` | Scheduling team |
| `CourseDirector` | `CourseDirector` | Training department leads |
| `Management` | `Management` | Directors, finance (read-only) |
| `SalesStaff` | `SalesStaff` | Sales and account management |
| `Instructor` | `Instructor` | All simulator instructors |
| `InternalStudent` | `InternalStudent` | Staff who book simulator time for themselves |

**Priority rule:** if a user belongs to multiple groups, the Lambda assigns the
highest-priority role from this ordered list:

```
SystemAdmin → ScheduleAdmin → CourseDirector → Management → SalesStaff → Instructor → InternalStudent
```

A user with **no matching group** is not provisioned — the Lambda logs a warning
and returns without creating an `app_users` row. The user will see an error on the
callback page.

---

## Step 5 — API permissions

Go to **API permissions → Add a permission → Microsoft Graph**:

**Delegated permissions** (for the hosted UI sign-in flow):

| Permission | Reason |
|---|---|
| `openid` | Required for OIDC |
| `email` | Email claim in ID token |
| `profile` | Name and groups claims in ID token |

**Application permissions** (for the Token Refresh Lambda — client credentials flow):

| Permission | Reason |
|---|---|
| `GroupMember.Read.All` | Re-sync group membership on token refresh |
| `User.Read.All` | Look up user by UPN/email |

Click **Grant admin consent for [your tenant]** for all permissions.

---

## Step 6 — Configure Cognito Staff Pool OIDC federation

In **AWS Console → Cognito → User Pools → fsbs-staff-pool →
Sign-in experience → Federated identity provider sign-in → Add an identity provider → OpenID Connect**:

| Field | Value |
|---|---|
| Provider name | `EntraID` |
| Client ID | `<Application (client) ID from Step 1>` |
| Client secret | `<Client secret from Step 2>` |
| Authorized scopes | `openid email profile` |
| Issuer URL | `https://login.microsoftonline.com/<Directory (tenant) ID>/v2.0` |

Cognito auto-discovers endpoints from the issuer's `.well-known/openid-configuration`.

**Attribute mapping** (Entra claim → Cognito user pool attribute):

| Entra ID token claim | Cognito attribute |
|---|---|
| `sub` | `username` |
| `email` | `email` |
| `groups` | `custom:entra_groups` |
| `given_name` | `given_name` |
| `family_name` | `family_name` |

---

## Step 7 — Configure the Cognito hosted UI

In **Cognito → App clients → fsbs-staff-app-client → Edit hosted UI**:

| Field | Value |
|---|---|
| Allowed callback URLs | `https://localhost:5001/auth/callback` and `https://staging.fsbs.tqaentry.com/auth/callback` |
| Allowed sign-out URLs | `https://localhost:5001/logout` and `https://staging.fsbs.tqaentry.com/logout` |
| Identity providers | `EntraID` only — disable Cognito native sign-in for the staff pool |
| OAuth 2.0 grant types | `Authorization code` |
| OpenID Connect scopes | `openid`, `email`, `profile` |

---

## Step 8 — Update application configuration

### API — `appsettings.Development.json`

To test the real Entra → Cognito flow on localhost, set `DevAuth:Enabled` to `false`:

```json
{
  "DevAuth": {
    "Enabled": false
  },
  "Cognito": {
    "AwsRegion": "eu-west-1",
    "StaffPoolId": "eu-west-1_XXXXXXXXX",
    "StaffClientId": "<Cognito app client ID for staff pool>"
  }
}
```

### Web — `wwwroot/appsettings.json`

```json
{
  "ApiBaseUrl": "https://localhost:5001",
  "Cognito": {
    "StaffPoolLoginUrl": "https://<cognito-domain>.auth.eu-west-1.amazoncognito.com/login?client_id=<staff-client-id>&response_type=code&scope=openid+email+profile&redirect_uri=https%3A%2F%2Fstaging.fsbs.tqaentry.com%2Fauth%2Fcallback"
  }
}
```

---

## Step 9 — Token Refresh Lambda (Entra group re-sync)

The Token Refresh Lambda calls Microsoft Graph on every token refresh to re-check
group membership and update the Cognito group if the user's Entra role has changed.
It uses the **client credentials flow** (no user context):

1. Store the client ID and secret in Secrets Manager as:
   - `fsbs/entra/client-id`
   - `fsbs/entra/client-secret`
   - `fsbs/entra/tenant-id`

2. The Lambda authenticates to Graph at:
   `https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/token`
   with `grant_type=client_credentials` and `scope=https://graph.microsoft.com/.default`

3. It then calls:
   `GET https://graph.microsoft.com/v1.0/users/{userId}/memberOf`
   to retrieve current group membership and compares against the user's current
   Cognito group, calling `AdminAddUserToGroup` / `AdminRemoveUserFromGroup` as needed.

---

## Checklist

- [ ] Entra app registration created (`FSBS Staff Portal`)
- [ ] Redirect URI set to Cognito `oauth2/idpresponse` endpoint
- [ ] Client secret created and stored in Secrets Manager
- [ ] ID token configured to emit `email`, `given_name`, `family_name`, `groups` (display names)
- [ ] Seven security groups created with names matching FSBS role strings exactly
- [ ] Staff users assigned to the appropriate group in Entra
- [ ] Microsoft Graph delegated permissions granted with admin consent
- [ ] Microsoft Graph application permissions granted with admin consent (for Token Refresh Lambda)
- [ ] Cognito OIDC federation configured pointing at Entra issuer URL
- [ ] Attribute mapping includes `groups` → `custom:entra_groups`
- [ ] Cognito hosted UI callback URLs include `https://localhost:5001/auth/callback`
- [ ] `StaffPoolLoginUrl` populated in `FSBS.Web/wwwroot/appsettings.json`
- [ ] `DevAuth:Enabled` set to `false` when testing real Entra flow
