# FSBS Implementation Status

Overall estimate: **~92% complete.** All six implementation phases finish below — domain, application, infrastructure, API, notification worker, and CDK (including Cognito Lambda bodies and the post-deploy DB grants custom resource). Remaining work is the test suite (Phase 7) and end-to-end UI verification.

---

## Well-implemented

| Area | Status |
|---|---|
| Database schema (`fsbs_schema.sql`) | ~95% — all tables, constraints, enums, RLS, triggers |
| Domain entities, events, value objects, exceptions, repository interfaces | Complete |
| Aggregate root + domain event dispatch (`MediatRDomainEventDispatcher`) | Complete |
| Application write side — `BookSimulatorSlot`, `Approve`, `Reject`, `Cancel`, validators | Complete |
| Application services — `PricingService`, `ReconfigurationService` | Complete |
| MediatR pipeline — Logging, Validation, Transaction behaviours | Complete |
| EF Core configurations (34 entity configs) + `FsbsDbContext` + audit/tenant interceptors | Complete |
| `IUnitOfWork` (`FsbsUnitOfWork`) — collects events from tracked `AggregateRoot`s, delegates commit to `FsbsDbContext` | Complete |
| Repositories — Booking, Invitation, Organisation, Simulator, ReconfigTemplate, ReconfigSlot, PricingPolicy, Instructor | Complete |
| Dapper read layer — `AvailabilityReadService` | Complete |
| Infrastructure services — `AvailabilityCache` (Redis), `SqsPublisher`, `SesEmailService`, `S3Service` | Complete |
| API — dual Cognito JWT auth + dev scheme, named role policies, `FsbsClaimsTransformation` | Complete |
| API endpoints — bookings (CRUD + approve/reject/cancel), pricing quote, simulator availability, invitations, organisations, auth | Complete |
| `AvailabilityHub` SignalR + Redis backplane + cache-invalidate-and-push helper | Complete |
| Notification worker — SQS consumer, message dispatcher, 6 event handlers, SES template seeder | Complete |
| Cognito Lambda bodies — `PreSignUp` (token validation), `PostConfirmation` (user provisioning + group membership), `TokenRefresh` (group sync + claim override) | Complete |
| Invitation event emission — `InvitationIssuedEvent` published to SQS by both invitation handlers; worker handler sends SES email with raw token | Complete |
| CDK — `NetworkStack`, `DataStack`, `AppStack` with ECS Fargate (api+worker), ALB, CloudFront, WAF, RDS Multi-AZ, Redis, SQS+DLQ, SNS, Cognito staff/customer pools, Lambda triggers wired, secrets rotation, alarms | Complete |
| CDK — DB grants custom resource provisions `fsbs_app` + `fsbs_readonly` roles via in-VPC Lambda; ECS services depend on it | Complete |
| Blazor Web UI — wizard, calendar, my bookings, invitations, Fluxor state slices, typed HTTP services | ~50% — page scaffolding present; binding to live API not fully verified |

---

## Not yet implemented

### Phase 7 — Tests (0% coverage)

All three test projects contain only a `UnitTest1` stub.

- [ ] **Domain unit tests** — booking state machine transitions, capacity validators (FlightDeck ≤4, CabinCrew ≤10), minimum 240-min duration, InternalStudent required-field rule, instructor rating intersection, pricing/discount evaluation, reconfiguration slot duration lookup, invitation token hashing + scope rules.
- [ ] **Application unit tests** — handler tests with mocked repositories: idempotency replay, reviewer ≠ booker guard, rejection reason length, reconfig slot insertion on approve, orphan cleanup on cancel/reject, `SlotBookedEvent` / `BookingApprovedEvent` emission, pricing snapshot immutability.
- [ ] **Integration tests** — Testcontainers PostgreSQL: verify CHECK constraints, partial unique indexes, RLS enforcement under multi-tenant context, `update_org_balance()` trigger correctness, end-to-end booking lifecycle through MediatR pipeline.

### Known follow-ups outside Phase 6 scope

- [ ] TokenRefresh Lambda does not yet call Microsoft Graph to detect disabled Entra accounts — federated sign-in already blocks new logins, but residual refresh-token windows remain. Wire Graph + `AdminUserGlobalSignOut` when needed.
- [ ] Nightly invitation-expiry sweep Lambda (mark `Pending` → `Expired` after 7 days).

### Blazor Web (deferred polish)

- [ ] Verify wizard end-to-end against live API (book → confirm → SignalR push → calendar refresh).
- [ ] Capacity indicator + reconfiguration window tooltip rendering.
- [ ] Role-adaptive nav coverage for all 10 `AppRole` values.

---

## Phase summary

| Phase | Status |
|---|---|
| Phase 1 — Domain foundation | ✅ Complete |
| Phase 2 — Application write side (booking flow) | ✅ Complete |
| Phase 3 — Infrastructure services | ✅ Complete |
| Phase 4 — API completion | ✅ Complete |
| Phase 5 — Notification worker | ✅ Complete |
| Phase 6 — CDK completion | ✅ Complete (grants custom resource + Cognito Lambdas wired with VPC, env, IAM) |
| Phase 6b — Cognito Lambda bodies | ✅ Complete (PreSignUp / PostConfirmation / TokenRefresh) |
| Phase 6c — Invitation event emission | ✅ Complete (`InvitationIssuedEvent` → SQS → SES) |
| Phase 7 — Tests | 🔴 0% coverage across all three test projects |

> **End-to-end checkpoint:** With Phase 6 complete and the unit of work wired up, booking commands now commit through MediatR's transactional pipeline, Cognito sign-up provisions an `fsbs.users` row, invitation links are emailed via SES with the raw token, RDS uses the least-privileged `fsbs_app` role at runtime, and staff token claims are kept in sync with Entra groups on every issuance. The remaining gap before production shipping is the regression-safety net (Phase 7).
