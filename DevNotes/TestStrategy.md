# FSBS Testing Strategy

Three-tier pyramid mapped to the existing test projects. Each tier has a clear role; tests don't bleed across layers.

## Tier 1 — `FSBS.Domain.Tests` (xUnit, fast, in-memory)

**Goal:** prove every business invariant in the domain layer is enforced regardless of caller.

**Stack:** xUnit + FluentAssertions + AutoFixture (or Bogus for realistic test data). No mocks needed — domain is pure.

**Coverage targets** (one test class per aggregate/rule cluster):

| Test class | Invariants under test |
|---|---|
| `BookingStateMachineTests` | All transitions in CLAUDE.md §state-machine. External path `Provisional → Confirmed → InProgress → Completed → Invoiced`. Internal path `PendingApproval → Confirmed/Rejected`. Negative: `InternalStudent` cannot enter `Provisional`; cannot transition `Completed → CancelledByCustomer`; rejection requires reason ≥10 chars. |
| `BookingCapacityValidatorTests` | `FlightDeck` ≤4, `CabinCrew` ≤10, boundary (=cap, cap+1), wrong training-type combinations. |
| `BookingDurationTests` | 240-min minimum (boundary tests at 239/240/241). |
| `InternalStudentRequiredFieldsTests` | `DepartmentName` + `BudgetCode` mandatory; null/whitespace rejected; not required for other roles. |
| `InstructorRatingMatchTests` | `TrainingTypeRatings` intersection with booking type — empty intersection rejected. |
| `PricingPolicyTests` | Base rate selected by `(config, training_type, customer_class, effective_date)`; effective-date filtering picks latest applicable. |
| `DiscountEvaluationTests` | Priority ordering, `IsCombinable` summing, max-cap, staff rate ignores discount rules. |
| `ReconfigurationTemplateTests` | Template lookup by `(from_config, to_config)`; fallback to `SimulatorUnit.DefaultReconfigMins`. |
| `InvitationTokenTests` | SHA-256 hashing produces correct hash; raw token never persisted on the entity; `MarkClaimed` only valid from `Pending`. |
| `BookingDiscountImmutabilityTests` | Snapshot cannot be mutated after `Confirmed`. |

**Conventions:**
- Test names: `Method_State_ExpectedOutcome` (e.g. `Confirm_WhenInternalStudentInProvisional_Throws`).
- Builders, not constructors: `BookingBuilder.ForInternalStudent().WithDuration(...).Build()`. Place under `tests/FSBS.Domain.Tests/Builders/`.
- One assert concept per test; use `[Theory]`+`[InlineData]` for boundary tables.

---

## Tier 2 — `FSBS.Application.Tests` (xUnit, mocked I/O)

**Goal:** exercise MediatR handlers and pipeline behaviours with all infrastructure faked.

**Stack:** xUnit + NSubstitute (or Moq) + FluentAssertions. Add `MediatR` directly so handlers can be invoked through a real `IMediator` for pipeline tests.

**Coverage targets:**

| Test class | What it proves |
|---|---|
| `BookSimulatorSlotHandlerTests` | Idempotency replay returns prior result without second insert; `SlotBookedEvent` raised; reconfig slot inserted when next booking has different config; `InternalStudent` skips Provisional → goes to PendingApproval. |
| `ApproveBookingHandlerTests` | Reviewer-≠-booker guard throws; reconfig slots inserted on approval; `BookingApprovedEvent` raised. |
| `RejectBookingHandlerTests` | Rejection reason length enforced; slot released; orphan reconfig slot deleted. |
| `CancelBookingHandlerTests` | Adjacent reconfig slots removed when no longer needed; cancellation event raised. |
| `PricingServiceTests` | Quote endpoint stateless; immutable snapshot written on `Confirmed`; price never recalculated after. |
| `ReconfigurationServiceTests` | Window inserted even with no subsequent booking; correct duration sourced. |
| `CreateInvitationHandlerTests` | CorporateManager scoped to own `tenant_id`; SalesStaff can scope to any org; duplicate active invitation rejected. |
| `ValidationBehaviourTests` | FluentValidation pipeline aborts before handler runs; aggregated error response. |
| `TransactionBehaviourTests` | Commands wrap in transaction; queries do not; failure rolls back. |
| `LoggingBehaviourTests` | Correlates logs with request ID. |

**Conventions:**
- One mock-setup helper per handler (e.g. `BookingHandlerFixture`) to keep individual tests readable.
- Verify event emission via spy on `IPublisher` — don't reach into the aggregate.
- For idempotency: assert repository called only once on replay.

---

## Tier 3 — `FSBS.Integration.Tests` (Testcontainers + WebApplicationFactory)

**Goal:** prove the system works end-to-end against real PostgreSQL and the real ASP.NET Core pipeline. This is where DB constraints, RLS, triggers, EF Core mappings, and JWT validation are exercised.

**Stack:** xUnit + Testcontainers.PostgreSql + Microsoft.AspNetCore.Mvc.Testing + Respawn (for fast per-test cleanup) + FluentAssertions. Replace AWS clients with LocalStack (or fakes registered in `WebApplicationFactory.ConfigureWebHost`).

**Fixture model:**
- One xUnit `ICollectionFixture<PostgresFixture>` shared across the assembly — single container, applied migrations once.
- `Respawn` truncates tables between tests in milliseconds. Avoid per-test container churn.
- Custom `FsbsWebApplicationFactory<TProgram>` that:
  - Swaps `IAmazonSQS`, `IAmazonSimpleEmailService`, `IAmazonS3` for in-memory fakes.
  - Replaces JWT scheme with a test scheme that mints `FsbsPrincipal` via header (`X-Test-Role: SalesStaff`, `X-Test-Tenant: <guid>`).
  - Points `DbContext` at the Testcontainers connection string.

**Coverage targets:**

| Test class | What it proves |
|---|---|
| `DatabaseConstraintTests` | Each CHECK fires: `duration_mins < 240` rejected; `student_count > 4` for FlightDeck rejected; reviewer == booker rejected; rejection without reason rejected. |
| `UniqueIndexTests` | Double-booking same `(bay, start, end)` rejected; duplicate `Pending` invitation per `(email, org)` rejected; reconfig template pair uniqueness. |
| `RowLevelSecurityTests` | Tenant A cannot read tenant B's bookings/invitations/invoices/courses/orgs/enrolments. Migration role bypasses RLS; `fsbs_app` role respects it. |
| `BalanceTriggerTests` | `update_org_balance()` recomputes correctly on invoice insert/update/delete and payment verify/void; nightly reconciliation matches trigger. |
| `BookingLifecycleTests` | `POST /v1/bookings` → Provisional → `POST /confirm` → reconfig slot persists → SignalR delta fires → cache invalidated. Internal-student variant: PendingApproval → Approve → reconfig inserted. |
| `IdempotencyTests` | Same `Idempotency-Key` returns identical response, single row written. |
| `AuthorizationPolicyTests` | Each named policy: matrix of `[role × endpoint]` returning 200/403 as expected. |
| `InvitationFlowTests` | `POST /invitations` → token hash stored, raw token returned only in response → `GET /invitations/validate` succeeds → `POST /register` triggers PreSignUp + PostConfirmation simulation → `AppUser` created. |
| `AvailabilityCacheTests` | First call hits DB, second hits Redis (use Testcontainers Redis); mutation invalidates the key. |
| `PricingQuoteTests` | `GET /pricing/quote` p95 latency budget acts as a regression guard (informational, not gating). |

**Conventions:**
- Test data via `DatabaseSeeder.SeedSimulators(...)` helpers — never hand-rolled SQL inside tests.
- Set `app.current_tenant_id` via the same middleware path the API uses; never as a raw `SET LOCAL` in test code.
- Use `WebApplicationFactory.CreateClient()` per test, not shared, so cookies/headers don't leak.

---

## What's explicitly out of scope (and why)

- **Cognito Lambda triggers** — test the handler logic in `FSBS.Application.Tests` against fakes; the Lambda host (`Function.cs`) is a thin shim and is covered by manual smoke tests in staging.
- **CDK stacks** — `cdk synth` in CI is sufficient. Don't unit-test infrastructure shape.
- **Blazor components** — defer bUnit until the wizard is wired to live API. Component tests have low ROI vs. the contract tests above.
- **End-to-end browser tests** — Playwright deferred to post-MVP.

## CI wiring

- One GitHub Actions job: `dotnet test FSBS.sln` on every PR.
- Testcontainers needs Docker — already available on `ubuntu-latest` runners. No Docker-in-Docker required.
- Coverage gate via `coverlet`: fail PR if Domain coverage <85% or Application coverage <70%. No gate on Integration (line coverage is misleading there).
- Two filters for fast local iteration: `dotnet test --filter Category=Unit` (Tiers 1+2) and `--filter Category=Integration` (Tier 3, slower).

## Suggested build order

1. Domain tests for state machine + validators (highest leverage; locks down core rules before refactors).
2. Database constraint integration tests (cheap, proves DDL).
3. Application handler tests (idempotency + reviewer guard + reconfig insertion).
4. RLS + trigger integration tests.
5. End-to-end booking lifecycle.
6. Authorization policy matrix.
