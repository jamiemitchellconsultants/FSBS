# CLAUDE.md — Flight Simulator Booking System (FSBS)

This file is the authoritative technical reference for the FSBS codebase. Read it in full before writing any code. All architectural decisions, naming conventions, business rules, and infrastructure choices are defined here and must be followed exactly.

---

## Project overview

A multi-tenant, role-based flight simulator booking platform for a flight training school. Two user populations: **Staff** (Entra ID auth) and **Customers** (Cognito auth). Supports simulator configuration management, booking with reconfiguration windows, instructor scheduling, student progress tracking, corporate account management, invitation-only customer registration, and management reporting.

---

## Solution structure

```
FSBS.sln
├── src/
│   ├── FSBS.Domain/           # Entities, value objects, domain events, aggregate roots, interfaces
│   ├── FSBS.Application/      # CQRS use cases (MediatR), DTOs, FluentValidation, IServices
│   ├── FSBS.Infrastructure/   # EF Core DbContext, repositories, AWS SDK clients, SES/SQS adapters
│   ├── FSBS.Api/              # ASP.NET Core 10 minimal API, controllers, middleware, SignalR hub
│   ├── FSBS.Web/              # Blazor WebAssembly SPA
│   │   ├── Pages/             # Routable page components (one folder per feature)
│   │   ├── Components/        # Shared UI components (calendar, wizard, capacity indicator)
│   │   ├── Services/          # Typed HttpClient wrappers, SignalR client, pricing quote service
│   │   ├── State/             # Fluxor store slices (booking wizard, calendar, pricing, session)
│   │   └── Layout/            # Shell, role-adaptive nav, breadcrumbs
│   └── FSBS.Shared/           # DTOs, enums, FluentValidation rules — shared by Api and Web
├── infrastructure/
│   └── FSBS.Cdk/              # AWS CDK in C# — NetworkStack, DataStack, AppStack
└── tests/
    ├── FSBS.Domain.Tests/
    ├── FSBS.Application.Tests/
    └── FSBS.Integration.Tests/
```

---

## Technology stack

| Layer | Technology |
|---|---|
| Frontend | Blazor WebAssembly (.NET 10) |
| API | ASP.NET Core 10 Web API (minimal API) |
| Architecture | Clean Architecture + CQRS (MediatR) |
| ORM | Entity Framework Core 10, Npgsql provider |
| Read queries | Dapper (complex joins that bypass EF) |
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
| Observability | CloudWatch + X-Ray |
| Frontend libs | MudBlazor, Fluxor, Blazored.LocalStorage, Polly |
| PDF generation | QuestPDF (server-side, called from API) |
| Validation | FluentValidation (pipeline behaviour in MediatR) |

---

## Authentication architecture

### Two Cognito User Pools — never merge them

**Staff Pool** (`fsbs-staff-pool`)
- Identity source: Microsoft Entra ID, registered as OIDC IdP in Cognito
- Entra attribute mapping: `oid` → `sub`, `email` → `email`, `groups` → `custom:entra_groups`
- Staff NEVER hold a Cognito-native password
- First sign-in: Post Confirmation Lambda reads `custom:entra_groups`, calls `AdminAddUserToGroup` to place user in the matching Cognito group
- Token refresh: Token Refresh Lambda calls Microsoft Graph API, updates Cognito group if Entra groups have changed
- Deprovisioning: Token Refresh Lambda calls `AdminUserGlobalSignOut` when Entra account is disabled
- Roles covered: SystemAdmin, ScheduleAdmin, CourseDirector, Instructor, Management, SalesStaff, InternalStudent

**Customer Pool** (`fsbs-customer-pool`)
- Self-signup: **DISABLED**. Pre Sign-up Lambda rejects any registration without a valid invitation token
- Private customers: open self-registration via a separate hosted UI flow (no invitation required)
- CorporateManagers: invitation from SalesStaff only, scoped to an existing Organisation
- CorporateStudents: invitation from CorporateManager only, scoped to their own org
- Optional per-org SAML 2.0 federation (does NOT bypass invitation requirement)
- Roles covered: PrivateCustomer, CorporateManager, CorporateStudent

### API JWT validation

```csharp
// Two schemes — both produce a unified FsbsPrincipal
builder.Services.AddAuthentication()
    .AddJwtBearer("Staff", options => { /* Staff Pool JWKS */ })
    .AddJwtBearer("Customer", options => { /* Customer Pool JWKS */ });
```

Use `IClaimsTransformation` to map `app_role` and `tenant_id` from either pool into `FsbsPrincipal`. The API never references Entra directly.

### Three Cognito Lambda triggers (deployed as separate Lambda functions)

1. **Pre Sign-up** — validates invitation token hash for Customer Pool sign-ups
2. **Post Confirmation** — creates `AppUser` record, assigns `org_id` and `app_role`, marks invitation Claimed, places staff in Cognito group
3. **Token Refresh** — re-syncs Cognito group membership from Entra groups (staff pool only)

---

## Roles and permissions

| Role | Pool | Auth source | Notes |
|---|---|---|---|
| SystemAdmin | Staff | Entra | Full access including payment void |
| ScheduleAdmin | Staff | Entra | Schedule, simulators, reconfig templates |
| CourseDirector | Staff | Entra | Courses, enrolments, progress sign-off |
| Instructor | Staff | Entra | Own schedule, debrief, assigned students |
| Management | Staff | Entra | Read-only: dashboards, reports, accounts |
| SalesStaff | Staff | Entra | Approve/reject internal bookings; record payments; issue CorporateManager invitations |
| InternalStudent | Staff | Entra | Own bookings (PendingApproval flow, dept + budget code required) |
| PrivateCustomer | Customer | Cognito | Self-service booking |
| CorporateManager | Customer | Cognito (invitation from SalesStaff) | Book for org, view account, issue student invitations |
| CorporateStudent | Customer | Cognito (invitation from CorporateManager) | Own bookings at org rate |

Use `[Authorize(Policy = "RequireSalesStaff")]` style named policies. Map `AppRole` enum values to policies in `Program.cs`.

---

## Domain model — aggregate roots

### SimulatorUnit
- Has many `SimulatorBays`
- Has an `ActiveConfigurationId` (FK to `SimulatorConfigurations`)
- Has `DefaultReconfigMins` (fallback when no `ReconfigurationTemplate` exists for a pair)

### SimulatorConfiguration
- Defines: `AircraftType`, `ConfigMode` (CockpitOnly | CockpitAndCabin), `SupportedTrainingTypes` (FlightDeck | CabinCrew)
- `MaxCapacityFlightDeck` = 4 (hard cap)
- `MaxCapacityCabinCrew` = 10 (hard cap)
- Referenced by `ReconfigurationTemplates`, `PricingPolicies`, `ScheduleTemplates`

### ReconfigurationTemplate
- UNIQUE constraint on `(from_config_id, to_config_id)`
- `DurationMins` is the turnaround time between those two configurations
- Falls back to `SimulatorUnit.DefaultReconfigMins` if no template exists

### Booking
- **Central aggregate** — owns `BookingSlots`, `ReconfigurationSlot`, `BookingNotes`, `BookingDiscounts`, `BookingApproval`
- Required fields when `BookerRole == InternalStudent`: `DepartmentName`, `BudgetCode`
- `IdempotencyKey` (UUID) required on POST — enforce via `Idempotency-Key` header
- Price locked at confirmation; never recalculated after `Confirmed`

### Booking state machine

```
External customers:
  → Provisional (15-min hold, slot reserved)
  → Confirmed (price locked)
  → InProgress → Completed → Invoiced

Internal students:
  → PendingApproval (no expiry, slot reserved immediately)
  → Confirmed (approved by SalesStaff)
  → Rejected (slot released, reason mandatory ≥10 chars)

Any → CancelledByCustomer | CancelledByAdmin | OnHold | Expired (Provisional only)
```

**Critical rules:**
- InternalStudent bookings NEVER enter Provisional. Skip straight to PendingApproval.
- A reviewer cannot be the same user as the booker (enforced in handler and API).
- On cancellation/rejection: re-evaluate adjacent bookings and remove orphaned `ReconfigurationSlots`.

### ReconfigurationSlot
- Non-billable buffer reserved immediately when a booking is confirmed
- Inserted after the confirmed booking if the next booking on that bay uses a different config
- Also inserted even with no subsequent booking (protects operational readiness)
- Rendered on calendar as hatched grey — non-selectable by users
- `DurationMins` sourced from `ReconfigurationTemplates` or `SimulatorUnit.DefaultReconfigMins`

### PricingPolicy / DiscountRule / BookingDiscount
- `PricingPolicy`: base rate per `(config_id, training_type, customer_class, effective_date)`
- `DiscountRule`: threshold-based rules with priority ordering and `IsCombinable` flag
- `BookingDiscount`: immutable snapshot written at confirm time — never updated
- Discount evaluation: collect applicable rules → sort by priority → apply highest (or sum combinable) → cap at configured maximum
- Staff rate: flat rate; no volume or advance discounts apply to InternalStudent bookings

### Invitation
- `TokenHash`: SHA-256 of the raw token — raw token NEVER stored
- `Status`: Pending → Claimed | Expired | Revoked
- UNIQUE partial index on `(invitee_email, org_id) WHERE status = 'Pending'`
- Expiry: 7-day default; nightly Lambda sweeps and marks Expired
- A CorporateManager cannot invite students to a different organisation (enforced by checking JWT `org_id`)

### Organisation / OrgAccount
- `OrgAccount` (1:1 with Organisation): `CurrentBalanceGbp` maintained by PostgreSQL trigger
- `AccountPayments`: created in `Pending` status by SalesStaff; `Verified` by Management/SystemAdmin
- Nightly reconciliation Lambda cross-checks trigger value vs. full sum query; raises CloudWatch alarm on discrepancy
- Payment verification is a hard requirement before balance is updated

---

## Database conventions (PostgreSQL 16, `fsbs` schema)

- **Naming**: snake_case via `EFCore.NamingConventions` (`UseSnakeCaseNamingConvention()`)
- **PKs**: `uuid` with `uuid_generate_v4()` default
- **Timestamps**: `timestamptz` (C# `DateTimeOffset`)
- **Soft deletes**: `is_deleted` boolean + EF Core global query filter
- **Audit columns**: `created_at`, `updated_at`, `created_by`, `updated_by` on every table via EF interceptor
- **Concurrency**: PostgreSQL `xmin` system column for optimistic concurrency
- **Migrations**: code-first; production applies via CI/CD pipeline only — never `dotnet ef database update` manually

### Critical DB-level constraints

```sql
-- Booking slot minimum duration
CHECK (duration_mins >= 240)

-- Crew-type capacity hard caps
CHECK (student_count <= 4)  WHERE training_type = 'FlightDeck'
CHECK (student_count <= 10) WHERE training_type = 'CabinCrew'

-- Internal student required fields
-- Enforced in domain validator (BookingCapacityValidator); also document in code

-- Unique booking slot (no double-booking)
UNIQUE (bay_id, start_at, end_at) WHERE slot_status != 'Cancelled'

-- Unique reconfig slot (no overlap)
UNIQUE (bay_id, start_at, end_at) ON reconfiguration_slots

-- Unique reconfiguration template pair
UNIQUE (from_config_id, to_config_id) ON reconfiguration_templates

-- Unique active invitation per email+org
UNIQUE (invitee_email, org_id) WHERE status = 'Pending'

-- Unique invitation token hash
UNIQUE (token_hash) ON invitations
```

### Multi-tenancy

Every tenant-scoped entity has `tenant_id`. Apply EF Core Global Query Filter:

```csharp
builder.HasQueryFilter(e => e.TenantId == _currentTenantId);
```

Inject `tenant_id` from the JWT into `DbContext` via middleware before any handler executes. Staff always operate in the school's root tenant.

---

## API conventions

- **Base URL**: `https://api.fsbs.example.com/v1`
- **Versioning**: URL prefix (`/v1/`, `/v2/`)
- **Pagination**: cursor-based — `?after=<cursor>&limit=<n>` (never offset)
- **Errors**: Problem Details RFC 7807 (`application/problem+json`)
- **Idempotency**: `Idempotency-Key` header (UUID) required on `POST /bookings`
- **Auth**: Cognito Bearer JWT on all endpoints except `/auth/*`, `/register`, `/invitations/validate`

### Key endpoint groups

| Group | Notes |
|---|---|
| `POST /register` | Public; invitation token in body; triggers Cognito sign-up |
| `GET /invitations/validate?token=` | Public; validates token before rendering registration form |
| `POST /invitations` | SalesStaff → CorporateManager; CorporateManager → CorporateStudent (own org only) |
| `GET /simulators/{id}/availability` | Returns `availableSlots[]`, `reconfigurationWindows[]`, `maintenanceWindows[]`; Redis cached 60s |
| `GET /pricing/quote` | Stateless price preview; call on every wizard step change |
| `POST /bookings` | Idempotency-Key required; InternalStudent → PendingApproval; others → Provisional |
| `PUT /bookings/{id}/approve` | SalesStaff/SystemAdmin; reviewer ≠ booker enforced |
| `PUT /bookings/{id}/reject` | Mandatory `reason` (min 10 chars) |
| `GET /bookings/pending-approval` | SalesStaff/SystemAdmin only |
| `POST /organisations/{id}/account/payments` | SalesStaff/SystemAdmin; creates Pending payment |
| `PUT /organisations/{id}/account/payments/{id}/verify` | Management/SystemAdmin only |
| `PUT /organisations/{id}/account/payments/{id}/void` | Management/SystemAdmin; mandatory reason |

Full OpenAPI spec: `fsbs-openapi.yaml` in the project root.

---

## Real-time: SignalR AvailabilityHub

- Hub name: `AvailabilityHub`
- Pushes slot-availability delta messages to all connected clients on booking create/modify/cancel
- Delta payload includes `reconfigurationWindows[]` so calendar renders them without a separate fetch
- Redis (ElastiCache) acts as the SignalR backplane — required because multiple Fargate tasks run concurrently
- Cache key invalidated on every mutation; 60-second TTL on availability grid

---

## CQRS pattern (MediatR)

Every operation is a Command or Query. No business logic in controllers or Blazor components.

```csharp
// Command example
public record BookSimulatorSlotCommand(...) : IRequest<BookingDto>;
public class BookSimulatorSlotHandler : IRequestHandler<BookSimulatorSlotCommand, BookingDto> { ... }

// Pipeline behaviours (register in order)
1. LoggingBehaviour<,>
2. ValidationBehaviour<,>      // FluentValidation — throws before handler if invalid
3. TransactionBehaviour<,>     // Wraps commands in a DB transaction
```

Domain events: emit from aggregate, handle in `INotificationHandler`, publish to SQS for async side-effects.

```
BookSimulatorSlotCommand
  → BookSimulatorSlotHandler
  → emits SlotBookedEvent
  → SQS
  → NotificationWorker (separate ECS task)
  → SES confirmation email
```

Complex read queries that bypass EF: use Dapper directly via `IDbConnection` injected into Query handlers.

---

## Blazor WASM conventions

- State management: **Fluxor** (Redux pattern) for multi-step wizard, calendar, pricing quote
- Never put business logic in components — call typed HttpClient services
- `FSBS.Shared` provides DTOs and enums for both client and server — no duplication
- JWT stored in `Blazored.LocalStorage`
- All API calls wrapped with Polly retry + circuit-breaker

### Booking wizard — step counts

| User type | Steps | Key difference |
|---|---|---|
| External customer | 7 | Ends with Provisional → customer confirms → Confirmed |
| Internal student | 8 | Step 7 = Department + Budget Code (both mandatory); ends with PendingApproval |

Wizard state persists across steps via Fluxor. Reset only on completion or explicit cancel.

### Availability calendar slot colours

| State | Colour | Behaviour |
|---|---|---|
| Available (>25% capacity) | Green | Selectable |
| Available (<25% capacity) | Amber | Selectable with tooltip |
| Reconfiguration window | Grey hatched | Non-selectable; tooltip shows transition + duration |
| Maintenance | Dark grey | Non-selectable |
| Booked by this user | Blue | Links to booking detail |
| Full | Red | Non-selectable |

---

## AWS infrastructure (CDK — C#)

Three CDK stacks in `FSBS.Cdk`:

| Stack | Contains |
|---|---|
| `NetworkStack` | VPC (3 AZs), public/private/isolated subnets, NAT GW per AZ, security groups |
| `DataStack` | RDS PostgreSQL Multi-AZ, ElastiCache Redis, S3 buckets, Secrets Manager entries |
| `AppStack` | ECS Fargate, CloudFront + WAF, ALB, Cognito pools, SQS/SNS, SES, ACM, CloudWatch |

### Key resource specs

| Resource | Spec |
|---|---|
| ECS Fargate (API) | 1 vCPU / 2 GB RAM; min 2 tasks, max 10; CPU scale-out at 60% |
| ECS Fargate (worker) | Separate service; polls SQS; no ALB registration |
| RDS | db.t4g.medium, Multi-AZ, 100 GB gp3, 7-day PITR, isolated subnet |
| ElastiCache | cache.t4g.small, private subnet, TLS in-transit |
| S3 | Two buckets: `fsbs-static` (Blazor WASM, OAC) and `fsbs-documents` (signed URLs only) |
| CloudFront | Price Class 100 (EU+NA); HTTPS only; custom domain; WAF WebACL attached |
| WAF | Rate limit 300 req/5 min per IP; OWASP Core Rule Set (SQLi + XSS managed rules) |
| ALB | Public subnet; security group permits inbound only from CloudFront managed prefix list |
| Cognito Lambdas | Three functions: Pre Sign-up, Post Confirmation, Token Refresh |
| ACM | Wildcard `*.fsbs.example.com`; auto-renewed |
| Secrets Manager | RDS credentials + API keys; 30-day rotation; injected as env vars at ECS startup |

### Deployment environments

| Env | Branch | DB |
|---|---|---|
| Development | `feature/*` | Local Docker PostgreSQL (docker-compose) |
| Staging | `develop` | RDS single-AZ t4g.micro; auto-deploy on merge |
| UAT | `release/*` | RDS single-AZ t4g.small; anonymised prod snapshot |
| Production | `main` | RDS Multi-AZ t4g.medium; manual approval gate in CodePipeline; blue/green |

---

## Business rules — must be enforced in domain layer

1. **Minimum booking duration**: 4 hours (240 minutes). Enforced by `BookingSlotValidator` AND `CHECK` constraint in DB.
2. **Flight Deck capacity**: max 4 students per booking. Enforced by `BookingCapacityValidator` AND DB `CHECK`.
3. **Cabin Crew capacity**: max 10 students per booking. Same enforcement pattern.
4. **Reconfiguration windows**: reserved at booking confirm time. Non-billable. Non-bookable.
5. **InternalStudent bookings**: `DepartmentName` and `BudgetCode` both required. No Provisional state. No discounts. Staff rate only.
6. **Approval self-assignment**: reviewer cannot be the same user as the booker.
7. **Instructor rating match**: instructor's `TrainingTypeRatings[]` must intersect the booking's `TrainingType`. Enforced in `BookSimulatorSlotHandler` and in `GET /instructors?trainingType=`.
8. **Invitation org scope**: CorporateManager invitation token is single-use. CorporateManager can only invite students to their own org (validated against JWT `tenant_id`).
9. **Payment verification**: payments created in `Pending` status. Balance only updated after `Verified`. Only Management/SystemAdmin can verify or void.
10. **Price immutability**: price locked at `Confirmed`. No recalculation after that state.
11. **Token security**: raw invitation tokens never stored. SHA-256 hash only. Pre Sign-up Lambda re-hashes the presented token for comparison.

---

## Notification events (async via SQS → SES/SNS)

Key events the notification worker must handle:

- `BookingConfirmed` — email + optional SMS to students and instructor
- `BookingPendingApproval` — email + dashboard alert to SalesStaff and SystemAdmin
- `BookingApproved` — email to InternalStudent
- `BookingRejected` — email to InternalStudent (includes rejection reason)
- `BookingReminder` — 24 hours before slot
- `ReconfigurationAlert` — when reconfig window < 60 min before session
- `InvitationIssued` — email to invitee (CorporateManager or CorporateStudent)
- `InvitationExpiring` — 24 hours before expiry, to issuer
- `PaymentRecorded` — email to CorporateManager (Pending status notice)
- `PaymentVerified` — email to CorporateManager with updated balance
- `PaymentVoided` — email to CorporateManager and recording SalesStaff (includes reason)
- `AccountBalanceWarning` — when balance exceeds 80% of credit limit
- `QualificationExpiring` — 30 and 7 days before expiry

---

## Non-functional targets

| NFR | Target |
|---|---|
| API p95 latency (booking endpoints) | < 400 ms |
| Availability calendar load (cached) | < 1 s |
| Pricing quote response | < 200 ms |
| Blazor WASM initial load (Brotli) | < 2.5 MB / < 3 s on 4G |
| API uptime | 99.9% monthly SLA |
| Concurrent booking users | 500 (ECS auto-scale target) |
| Data retention | 7 years (UK regulatory) |
| GDPR erasure response | 72 hours; pseudonymisation API at `DELETE /users/{id}` |
| Accessibility | WCAG 2.1 Level AA |
| Browser support | Chrome 109+, Edge 109+, Firefox 112+, Safari 16+ |
| Audit | Immutable audit log on every write (user + timestamp) |
| Security | Annual third-party pentest; critical findings < 30 days |

---

## Key files to check first when working in each area

| Area | Files |
|---|---|
| Booking creation | `FSBS.Application/Bookings/Commands/BookSimulatorSlotCommand.cs` |
| Approval workflow | `FSBS.Application/Bookings/Commands/ApproveBookingCommand.cs` |
| Pricing engine | `FSBS.Application/Pricing/Services/PricingService.cs` |
| Reconfig logic | `FSBS.Application/Bookings/Services/ReconfigurationService.cs` |
| Availability cache | `FSBS.Infrastructure/Availability/AvailabilityCache.cs` |
| SignalR hub | `FSBS.Api/Hubs/AvailabilityHub.cs` |
| JWT claims | `FSBS.Api/Auth/FsbsClaimsTransformation.cs` |
| EF DbContext | `FSBS.Infrastructure/Persistence/FsbsDbContext.cs` |
| Global query filters | `FSBS.Infrastructure/Persistence/Configurations/` |
| Invitation token | `FSBS.Application/Invitations/Commands/CreateInvitationCommand.cs` |
| Pre Sign-up Lambda | `FSBS.Cdk/Lambdas/PreSignUpFunction/` |
| CDK stacks | `FSBS.Cdk/Stacks/` |

---

## What not to do

- Never store raw invitation tokens — SHA-256 hash only
- Never put business logic in Blazor components or API controllers
- Never use offset pagination — cursor-based only
- Never run `dotnet ef database update` against production manually
- Never bypass the FluentValidation pipeline in MediatR
- Never allow a reviewer to approve their own booking
- Never apply discounts to InternalStudent bookings
- Never allow a booking to enter Provisional state for an InternalStudent
- Never allow a CorporateManager to invite users into a different org
- Never calculate prices after a booking is Confirmed
- Never allow a ReconfigurationSlot to be booked by a user
- Never serve the document S3 bucket through CloudFront — pre-signed URLs only
- Never give ECS tasks a public IP
- Never put long-lived AWS credentials in the codebase — Secrets Manager only

---

## PostgreSQL DDL decisions

The canonical schema is in `fsbs_schema.sql`. Key decisions explained below.

### Extensions and schema

`uuid-ossp` provides `uuid_generate_v4()` defaults on every PK. `pgcrypto` is available for any server-side hashing needs. Everything lives in the `fsbs` schema — never `public`.

### Enums

All `BookingStatus`, `TrainingType`, `ConfigurationMode`, `DiscountType` etc. are native PostgreSQL ENUMs. EF Core uses the Npgsql enum mapping (`MapEnum<T>()`). Values are type-safe at the DB level; no `CHECK` constraints needed to enumerate string values.

### Critical CHECK constraints

These four rules are enforced at the storage layer regardless of how data enters the database:

```sql
-- booking_slots: minimum 4-hour rule
CHECK (duration_mins >= 240)

-- bookings: crew-type capacity hard caps
CHECK (training_type != 'FlightDeck' OR student_count <= 4)
CHECK (training_type != 'CabinCrew'  OR student_count <= 10)

-- booking_approvals: self-approval ban + rejection reason minimum length
CHECK (requested_by != reviewed_by)
CHECK (decision != 'Rejected' OR (rejection_reason IS NOT NULL AND char_length(rejection_reason) >= 10))
```

All four are annotated with `COMMENT ON CONSTRAINT` so they are self-documenting in `pg_catalog`.

### Invitation security

- `token_hash` is `char(64)` (SHA-256 hex) with a `UNIQUE` constraint — raw tokens are never stored
- Partial unique index on `(invitee_email, org_id) WHERE status = 'Pending'` prevents duplicate active invitations without blocking re-invitations after expiry or revocation
- `CHECK` constraints enforce that `claimed_by` / `revoked_by` are only populated when status warrants it

### booking_discounts immutability

`booking_discounts` has no `updated_at` and no `is_deleted` column. It is an immutable audit snapshot written once at booking confirmation and never modified. In EF Core, configure this entity to throw on any attempted update. The table comment in the DDL marks this explicitly.

### Deferred foreign key

`payment_allocations.invoice_id` FK is added via `ALTER TABLE` after `invoices` is created. `payment_allocations` is defined structurally within the payments section, but `invoices` comes later in the script. Do not reorder — maintain the deferred `ALTER TABLE` pattern.

### Balance trigger

`update_org_balance()` fires `AFTER INSERT OR UPDATE OR DELETE` on both `invoices` and `account_payments`. It recomputes `org_accounts.current_balance_gbp` as:

```
current_balance = SUM(net_gbp WHERE invoice status IN ('Issued','Overdue'))
                - SUM(amount_gbp WHERE payment status = 'Verified')
```

The nightly Lambda reconciliation job (deployed as a scheduled ECS task) cross-checks this trigger value against a full `SUM` query and raises a CloudWatch alarm on any discrepancy. Never bypass the trigger by writing directly to `current_balance_gbp`.

### Row-level security

RLS is enabled on the six tenant-scoped tables: `bookings`, `enrolments`, `courses`, `organisations`, `invitations`, `invoices`. The pattern policy reads `current_setting('app.current_tenant_id')::uuid`.

The EF Core `FsbsDbContext` middleware sets this via `SET LOCAL app.current_tenant_id = '<id>'` at the start of each request using the `tenant_id` claim extracted from the JWT by `FsbsClaimsTransformation`. Staff always operate with the school's root `tenant_id`.

The ECS task DB user (`fsbs_app`) is subject to RLS. The migration runner uses a superuser role that bypasses RLS — never run migrations with the application role.

### Grants

The grants section in `fsbs_schema.sql` is commented out (no hardcoded credentials). The CDK post-deployment custom resource runs the grant script against RDS using credentials pulled from Secrets Manager. Two roles are defined:

- `fsbs_app` — `SELECT`, `INSERT`, `UPDATE`, `DELETE` on all tables; used by ECS tasks
- `fsbs_readonly` — `SELECT` only; used by reporting dashboards and the Management role
