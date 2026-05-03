# Flight Simulator Booking System (FSBS)

A multi-tenant, role-based flight simulator booking platform for a flight training school. Two user populations: **Staff** (Microsoft Entra ID auth) and **Customers** (Amazon Cognito auth). Supports simulator configuration management, booking with reconfiguration windows, instructor scheduling, student progress tracking, corporate account management, invitation-only customer registration, and management reporting.

---

## Technology stack

| Layer | Technology |
|---|---|
| Frontend | Blazor WebAssembly (.NET 10) |
| API | ASP.NET Core 10 minimal API |
| Architecture | Clean Architecture + CQRS (MediatR) |
| ORM | Entity Framework Core 10, Npgsql provider |
| Read queries | Dapper |
| Database | PostgreSQL 16 (RDS Multi-AZ) |
| Auth — Staff | Microsoft Entra ID → Cognito Staff Pool (OIDC federation) |
| Auth — Customers | Amazon Cognito Customer Pool (invitation-only for corporate roles) |
| Cache | Amazon ElastiCache Redis |
| Messaging | Amazon SQS + SNS |
| Email | Amazon SES |
| File storage | Amazon S3 |
| CDN | Amazon CloudFront + WAF |
| Infrastructure | AWS CDK (C#) |
| CI/CD | GitHub Actions + AWS CodeDeploy (blue/green) |
| Frontend libs | MudBlazor, Fluxor, Blazored.LocalStorage, Polly |

---

## Solution structure

```
FSBS.sln
├── src/
│   ├── FSBS.Domain/                                    # Entities, enums, domain interfaces
│   ├── FSBS.Application/                               # CQRS commands/queries (MediatR), FluentValidation
│   ├── FSBS.Infrastructure/                            # Cognito, SES, SQS adapters
│   ├── FSBS.Infrastructure.Persistence/                # EF Core DbContext, interceptors
│   ├── FSBS.Infrastructure.Persistence.Entities/       # IEntityTypeConfiguration files
│   ├── FSBS.Infrastructure.Persistence.Migrations/     # EF Core migrations
│   ├── FSBS.Infrastructure.Persistence.Repositories/   # Repository implementations
│   ├── FSBS.Infrastructure.Persistence.Repositories.Interfaces/
│   ├── FSBS.Api/                                       # Minimal API endpoints, middleware, auth
│   ├── FSBS.Web/                                       # Blazor WASM SPA
│   │   ├── Pages/                                      # Routable pages (one folder per feature)
│   │   ├── Components/                                 # Shared UI components
│   │   ├── Services/                                   # Typed HttpClient wrappers, SignalR client
│   │   ├── State/                                      # Fluxor store slices
│   │   └── Layout/                                     # Shell, role-adaptive nav
│   ├── FSBS.Shared/                                    # DTOs and enums shared by Api and Web
│   └── FSBS.Functions/                                 # Cognito Lambda triggers
├── infrastructure/
│   └── FSBS.Cdk/                                       # AWS CDK stacks (Network, Data, App)
└── tests/
    ├── FSBS.Domain.Tests/
    ├── FSBS.Application.Tests/
    └── FSBS.Integration.Tests/
```

---

## Local development guide

No AWS or Azure account required.

---

## Prerequisites

| Tool | Version | Install |
|---|---|---|
| .NET SDK | 10.0+ | https://dotnet.microsoft.com/download |
| Docker Desktop | 4.x+ | https://www.docker.com/products/docker-desktop |
| EF Core CLI | latest | `dotnet tool install -g dotnet-ef` |

---

## 1. Start local infrastructure

```bash
docker compose up -d
```

This starts three containers:

| Service | Port | Purpose |
|---|---|---|
| PostgreSQL 16 | 5432 | Primary database (`fsbs` database, user `postgres`, password `localdev`) |
| Redis 7 | 6379 | SignalR backplane + availability cache |
| Mailpit | SMTP 1025 / Web 8025 | Catches outbound email — view at http://localhost:8025 |

Wait until all containers are healthy:

```bash
docker compose ps
```

---

## 2. Apply database migrations

```bash
dotnet ef database update \
  -p src/FSBS.Infrastructure.Persistence.Migrations \
  -s src/FSBS.Infrastructure.Persistence.Migrations
```

This creates the `fsbs` schema and all tables. The connection string is hardcoded in `FsbsDbContextFactory` for design-time use (`Host=localhost;Port=5432;Database=fsbs;Username=postgres;Password=localdev`).

---

## 3. Run the API

```bash
dotnet run --project src/FSBS.Api
```

The API starts on `https://localhost:5001` (HTTPS) and `http://localhost:5000` (HTTP).

OpenAPI docs: https://localhost:5001/openapi/v1.json

The dev environment activates automatically because `ASPNETCORE_ENVIRONMENT` defaults to `Development` when running via `dotnet run`.

---

## 4. Run the Blazor frontend

In a second terminal:

```bash
dotnet run --project src/FSBS.Web
```

The WASM app starts on `https://localhost:7001`. Navigate there in a browser.

---

## 5. Authenticate locally

The dev environment replaces AWS Cognito with a local JWT scheme. Two steps are needed: seed a user record in the database, then get a signed token.

### Step A — Seed a user

```bash
curl -s -X POST "https://localhost:5001/dev/users/seed?email=admin@fsbs.local&role=SystemAdmin" \
  -k | jq .
```

Response:

```json
{
  "userId": "...",
  "tenantId": "...",
  "email": "admin@fsbs.local",
  "role": "SystemAdmin"
}
```

Keep the `userId` and `tenantId` — they identify this user's database row.

### Step B — Get a dev token

```bash
curl -s -X POST "https://localhost:5001/dev/auth/token?email=admin@fsbs.local&role=SystemAdmin" \
  -k | jq -r .token
```

The token is a signed JWT valid for 8 hours. Use it as a `Bearer` token on any API request:

```bash
TOKEN=$(curl -s -X POST "https://localhost:5001/dev/auth/token?email=admin@fsbs.local&role=SystemAdmin" \
  -k | jq -r .token)

curl -s -H "Authorization: Bearer $TOKEN" https://localhost:5001/... -k
```

> The seed step and the token step use independent IDs. The token's `sub` claim is a random UUID generated at token-issue time and does not match the seeded `userId`. This is fine for development — all that matters is the `app_role` claim for authorization. When the real Cognito integration is wired up, these will be unified via `CognitoSub`.

---

## 6. Seed common users

Seed one user per role for comprehensive local testing:

```bash
for ROLE in SystemAdmin ScheduleAdmin CourseDirector Instructor Management SalesStaff InternalStudent PrivateCustomer; do
  curl -s -X POST "https://localhost:5001/dev/users/seed?email=${ROLE,,}@fsbs.local&role=$ROLE" -k
  echo
done
```

Then get a token for whichever role you want to test:

```bash
curl -s -X POST "https://localhost:5001/dev/auth/token?email=salesstaff@fsbs.local&role=SalesStaff" \
  -k | jq .
```

---

## 7. Test the registration flow

The registration endpoints (`POST /v1/auth/register`, `POST /v1/auth/confirm`) hit `ICognitoService`. In development, this is wired to `StubCognitoService` which returns `Task.CompletedTask` for all calls — no Cognito calls are made.

The Blazor pages at `/register` and `/register/confirm` call these endpoints. You can walk through the UI flow; the confirmation code input accepts any 6-digit value (the stub ignores it).

---

## 8. Test the invitation flows

There are two invitation paths. Both require an org to exist in the database first.

### Create an organisation

Organisations must be seeded directly via SQL or a future admin endpoint. For now, insert one manually:

```bash
# Connect to the local database
docker exec -it $(docker compose ps -q postgres) \
  psql -U postgres -d fsbs -c "
    INSERT INTO fsbs.organisations (id, tenant_id, name, customer_class, is_deleted, created_at, updated_at)
    VALUES (
      'aaaaaaaa-0000-0000-0000-000000000001',
      'bbbbbbbb-0000-0000-0000-000000000001',
      'Acme Airlines',
      'Corporate',
      false,
      now(),
      now()
    );
  "
```

Keep the org ID (`aaaaaaaa-0000-0000-0000-000000000001`) — you will need it below.

### SalesStaff inviting a CorporateManager

SalesStaff supply the org ID explicitly in the request body.

```bash
# 1. Seed + get a SalesStaff token
curl -s -X POST "https://localhost:5001/dev/users/seed?email=salesstaff@fsbs.local&role=SalesStaff" -k
STAFF_TOKEN=$(curl -s -X POST \
  "https://localhost:5001/dev/auth/token?email=salesstaff@fsbs.local&role=SalesStaff" \
  -k | jq -r .token)

# 2. Issue the invitation
curl -s -X POST https://localhost:5001/v1/invitations \
  -H "Authorization: Bearer $STAFF_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"inviteeEmail":"manager@acme.com","orgId":"aaaaaaaa-0000-0000-0000-000000000001"}' \
  -k | jq .
```

The UI page for this flow is at `https://localhost:7001/staff/invitations/corporate`.

### CorporateManager inviting a CorporateStudent

The CorporateManager's org is read from their JWT `org_id` claim — they cannot invite users into a different org.

```bash
# 1. Seed + get a CorporateManager token with org_id set
curl -s -X POST "https://localhost:5001/dev/users/seed?email=manager@acme.local&role=CorporateManager" -k
MGR_TOKEN=$(curl -s -X POST \
  "https://localhost:5001/dev/auth/token?email=manager@acme.local&role=CorporateManager&orgId=aaaaaaaa-0000-0000-0000-000000000001" \
  -k | jq -r .token)

# 2. Issue the student invitation
curl -s -X POST https://localhost:5001/v1/invitations/students \
  -H "Authorization: Bearer $MGR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"inviteeEmail":"student@acme.com"}' \
  -k | jq .
```

The UI page for this flow is at `https://localhost:7001/organisation/invitations`.

**Error cases to verify:**

| Scenario | Expected response |
|---|---|
| Duplicate pending invite | 409 Conflict |
| Unknown org ID (SalesStaff flow) | 404 Not Found |
| CorporateManager token without `org_id` | 500 (misconfigured token) |
| Invalid email format | 422 Unprocessable Entity |

### Accepting an invitation (CorporateManager or CorporateStudent)

The invitation response includes `invitationId` but not the raw token — the raw token only ever exists in the email. For local testing, you can grab it directly from the database:

```bash
# Find the raw token hash for a pending invitation
docker exec -it $(docker compose ps -q postgres) \
  psql -U postgres -d fsbs -c "
    SELECT id, invitee_email, token_hash, expires_at
    FROM fsbs.invitations
    WHERE status = 'Pending'
    ORDER BY created_at DESC LIMIT 5;
  "
```

The raw token is **not stored** — only the SHA-256 hash is. To test the claim flow locally without a real email, issue a token with a known value:

```bash
# Issue an invitation using a known raw token (dev only)
KNOWN_TOKEN=$(python3 -c "import secrets; print(secrets.token_hex(32))")
TOKEN_HASH=$(python3 -c "import hashlib,sys; print(hashlib.sha256(bytes.fromhex(sys.argv[1])).hexdigest())" "$KNOWN_TOKEN")
echo "Raw token:  $KNOWN_TOKEN"
echo "Token hash: $TOKEN_HASH"

# Insert the invitation directly with the known hash
docker exec -it $(docker compose ps -q postgres) \
  psql -U postgres -d fsbs -c "
    INSERT INTO fsbs.invitations
      (id, org_id, invitee_email, invitee_role, token_hash, status, expires_at, created_at, updated_at)
    VALUES (
      gen_random_uuid(),
      'aaaaaaaa-0000-0000-0000-000000000001',
      'newmanager@acme.com',
      'CorporateManager',
      '$TOKEN_HASH',
      'Pending',
      now() + interval '7 days',
      now(), now()
    );
  "
```

**Validate the token** (what the Blazor page calls on load):

```bash
curl -s "https://localhost:5001/v1/invitations/validate?token=$KNOWN_TOKEN" -k | jq .
# { "isValid": true, "inviteeEmail": "newmanager@acme.com", "orgName": "Acme Airlines", "role": "CorporateManager" }
```

**Claim the invitation** (what the form submits):

```bash
curl -s -X POST https://localhost:5001/v1/auth/register/invited \
  -H "Content-Type: application/json" \
  -d "{\"token\":\"$KNOWN_TOKEN\",\"password\":\"Password1!\",\"firstName\":\"Jane\",\"lastName\":\"Smith\"}" \
  -k | jq .
# { "userId": "...", "email": "newmanager@acme.com", "orgId": "...", "role": "CorporateManager" }
```

**Full UI flow**: navigate to `https://localhost:7001/invitations/claim?token=<KNOWN_TOKEN>` to walk through the registration form.

**Error cases for the claim flow:**

| Scenario | Expected response |
|---|---|
| Token already claimed | 404 (same as invalid — no enumeration) |
| Token expired | 404 |
| Invalid hex token format | 422 Unprocessable Entity |
| Passwords do not match | Client-side validation error |

---

## 9. View sent emails

All emails sent by the API (invitation emails, booking confirmations, etc.) are caught by Mailpit:

**http://localhost:8025**

No emails leave your machine.

---

## 10. Stop local infrastructure

```bash
docker compose down
```

To also delete the database volume (start fresh):

```bash
docker compose down -v
```

---

## Role reference

| Role | Auth pool | Notes |
|---|---|---|
| `SystemAdmin` | Staff | Full access |
| `ScheduleAdmin` | Staff | Simulators, schedule, reconfig templates |
| `CourseDirector` | Staff | Courses, enrolments, progress |
| `Instructor` | Staff | Own schedule and assigned students |
| `Management` | Staff | Read-only dashboards and reports |
| `SalesStaff` | Staff | Approve bookings, record payments, issue corporate invitations |
| `InternalStudent` | Staff | Own bookings (PendingApproval flow, requires dept + budget code) |
| `PrivateCustomer` | Customer | Self-service booking |
| `CorporateManager` | Customer | Book for org, view account, invite students |
| `CorporateStudent` | Customer | Own bookings at corporate rate |

---

## Configuration files

| File | Purpose |
|---|---|
| `src/FSBS.Api/appsettings.Development.json` | API dev config — DB connection string, dev JWT secret/issuer, SMTP |
| `src/FSBS.Web/wwwroot/appsettings.json` | Blazor config — API base URL, Cognito login URLs (unused in dev) |
| `docker-compose.yml` | Local infrastructure definitions |

---

## Troubleshooting

**`Connection refused` on migrations** — ensure `docker compose up -d` completed and postgres is healthy before running `dotnet ef database update`.

**`SSL certificate error` in curl** — use `-k` flag (shown in examples above) to skip local cert validation, or trust the dev cert: `dotnet dev-certs https --trust`.

**`DevAuth:Secret is not configured`** — the API must run in `Development` environment. Confirm `ASPNETCORE_ENVIRONMENT=Development` or that `launchSettings.json` sets it (default for `dotnet run`).

**Port conflict** — if 5432 or 6379 are in use, stop the conflicting service or edit the port mappings in `docker-compose.yml` and update the connection string in `appsettings.Development.json` to match.
