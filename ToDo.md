# FSBS Implementation Status

Overall estimate: ~45% complete. Core architecture and domain model are solid; business logic and AWS integrations are largely absent.

---

## Well-implemented

| Area | Status |
|---|---|
| Database schema (`fsbs_schema.sql`) | ~95% — all tables, constraints, enums, RLS, triggers |
| Domain entities | ~85% — all 30+ entities present with full properties |
| EF Core configurations | Complete — all 34 entity configs |
| Blazor Web UI | ~50% — pages, layouts, Fluxor state slices, client services |
| Auth scaffolding | Cognito SDK integration, dev auth scheme, validators for register/confirm |
| CQRS query handlers | Booking queries, invitation queries, organisation listing |
| Infrastructure persistence | DbContext, AuditInterceptor, 3 repositories (Booking, Invitation, Organisation) |

---

## Not yet implemented

### Application layer — core business logic

- [ ] `BookSimulatorSlotCommand` — booking creation
- [ ] `ApproveBookingCommand` / `RejectBookingCommand` / `CancelBookingCommand`
- [ ] `PricingService` — quote generation, discount calculation
- [ ] `ReconfigurationService` — automatic reconfiguration slot creation on confirm
- [ ] `TransactionBehaviour` — MediatR pipeline DB transaction wrapper
- [ ] Domain events — directory is empty (`SlotBookedEvent`, etc.)
- [ ] Value objects — directory exists but is empty

### Infrastructure layer

- [ ] `AvailabilityCache` — Redis/ElastiCache client
- [ ] SQS, SNS, SES, S3 clients
- [ ] Notification worker — async SQS consumer for email/SMS side-effects
- [ ] Repositories for remaining aggregates (SimulatorUnit, SimulatorBay, PricingPolicy, and ~9 others)

### API layer

- [ ] `FsbsClaimsTransformation` — JWT → `FsbsPrincipal` claims mapping
- [ ] `AvailabilityHub` — SignalR hub (Hubs directory is empty)
- [ ] Dual JWT validation for Staff + Customer Cognito pools (not in `Program.cs`)
- [ ] `POST /bookings`, `PUT /bookings/{id}/approve`, and all other write endpoints (currently empty shells)

### CDK / infrastructure

- [ ] Verify and complete ECS task definitions and auto-scaling policies
- [ ] Redis/ElastiCache cluster configuration
- [ ] SQS queue and SNS topic wiring

### Tests

- [ ] All three test projects contain only an empty stub — 0% coverage

---

## Critical path to functional MVP

1. `PricingService` (prerequisite for booking creation)
2. `BookSimulatorSlotCommand` + handler
3. `ReconfigurationService`
4. `ApproveBookingCommand` / `RejectBookingCommand`
5. `TransactionBehaviour` in the MediatR pipeline
6. `FsbsClaimsTransformation` + dual-pool JWT validation
7. `AvailabilityHub` (SignalR + Redis backplane)
8. SQS/SNS notification publisher + worker
9. Remaining repositories

---

## Step-by-step implementation plan

Build from the inside out (domain → application → infrastructure → API → worker → tests) so each layer has a solid foundation before the next is added.

### Phase 1 — Domain foundation ✅

Unblocks everything else. The application layer can't emit or handle events, and can't enforce invariants cleanly, until these exist.

- [x] Domain event base class — `IDomainEvent`, `AggregateRoot` base with `AddDomainEvent()` / `ClearDomainEvents()`
- [x] Key domain events — `SlotBookedEvent`, `BookingConfirmedEvent`, `BookingApprovedEvent`, `BookingRejectedEvent`, `BookingCancelledEvent`, `InvitationClaimedEvent`
- [x] Value objects — `Money`, `DateTimeRange`, `IdempotencyKey`
- [x] Domain interfaces — `IBookingRepository`, `IInvitationRepository`, `ISimulatorRepository`, `IPricingPolicyRepository`, `IReconfigurationTemplateRepository`, `IInstructorRepository`, `IOrganisationRepository`, `IUnitOfWork`, `IDomainEventDispatcher`
- [x] Domain exceptions — `DomainException`, `BookingConflictException`, `InvalidBookingStateTransitionException`
- [x] `Booking` and `Invitation` promoted to `AggregateRoot`

### Phase 2 — Application write side (core booking flow)

Each item depends on the previous.

- [x] `TransactionBehaviour<,>` — register third in the MediatR pipeline; wraps every command in a DB transaction
- [x] `PricingService` — pricing policy lookup, discount rule evaluation, price snapshot; must exist before any booking can be confirmed
- [x] `ReconfigurationService` — given a confirmed booking, determine whether a reconfiguration slot is needed, calculate duration from template or `DefaultReconfigMins`, return the slot to insert
- [x] `BookSimulatorSlotCommand` + handler — provisional/pending-approval branching, idempotency key check, emit `SlotBookedEvent`
- [x] `BookingCapacityValidator` + `BookingSlotValidator` — FluentValidation for capacity caps, minimum duration, InternalStudent required fields
- [x] `ApproveBookingCommand` + handler — reviewer ≠ booker guard, state transition, emit `BookingApprovedEvent`
- [x] `RejectBookingCommand` + handler — reason length guard, release slot, remove orphaned reconfig slots, emit `BookingRejectedEvent`
- [x] `CancelBookingCommand` + handler — customer and admin variants, reconfig slot cleanup

### Phase 3 — Infrastructure services

- [ ] Remaining repositories — SimulatorUnit, SimulatorBay, SimulatorConfiguration, ReconfigurationTemplate, PricingPolicy, DiscountRule, Instructor
- [ ] `AvailabilityCache` — Redis wrapper around availability grid; 60-second TTL; invalidate on every booking mutation
- [ ] `ISqsPublisher` + implementation — generic publish-to-SQS adapter used by domain event handlers
- [ ] `ISesEmailService` + implementation — SES send wrapper with template support
- [ ] `IS3Service` — signed URL generation for document bucket
- [ ] Dapper read layer — complex availability queries that bypass EF (slot grid, reconfig windows, maintenance windows in one query)

### Phase 4 — API completion

- [ ] `FsbsClaimsTransformation` — map `app_role` + `tenant_id` from either pool's JWT into `FsbsPrincipal`; register `IClaimsTransformation`
- [ ] Dual-pool JWT validation — add both `AddJwtBearer("Staff", ...)` and `AddJwtBearer("Customer", ...)` in `Program.cs`; configure policy to accept either
- [ ] Named authorization policies — one policy per `AppRole` enum value
- [ ] `AvailabilityHub` — SignalR hub; push availability delta (including `reconfigurationWindows[]`) on every booking mutation; wire Redis as backplane
- [ ] Wire write endpoints — `POST /bookings`, `PUT /bookings/{id}/approve`, `PUT /bookings/{id}/reject`, `POST /organisations/{id}/account/payments`, etc.; return Problem Details on failure
- [ ] `GET /simulators/{id}/availability` — Dapper read query, cached via `AvailabilityCache`, return `availableSlots[]` + `reconfigurationWindows[]` + `maintenanceWindows[]`
- [ ] `GET /pricing/quote` — stateless; call `PricingService` directly, no DB write

### Phase 5 — Notification worker

- [ ] SQS consumer loop — separate ECS service; poll queue, deserialise event envelope, dispatch to handlers
- [ ] Notification handlers — one per event: `BookingConfirmedHandler`, `BookingPendingApprovalHandler`, `BookingApprovedHandler`, `BookingRejectedHandler`, `BookingReminderHandler`, `InvitationIssuedHandler`, etc.
- [ ] Email templates — SES template registration for each notification type

### Phase 6 — CDK completion

- [ ] Review all three stacks against spec — ECS task/service definitions, auto-scaling policy (CPU 60%), RDS Multi-AZ config, Redis cluster, ALB security group (CloudFront prefix list only)
- [ ] SQS queues + SNS topics — booking events queue, notification worker subscription, dead-letter queue with alarm
- [ ] Lambda trigger wiring — confirm Pre Sign-up, Post Confirmation, Token Refresh are attached to the correct Cognito pools in `AppStack`
- [ ] Secrets Manager rotation — 30-day rotation enabled, injected as ECS env vars
- [ ] CDK post-deployment custom resource — runs the DB grants script (`fsbs_app` / `fsbs_readonly` roles)

### Phase 7 — Tests

Write alongside or immediately after each phase so failures are caught before the next phase builds on top.

- [ ] Domain unit tests — booking state machine transitions, capacity validators, pricing/discount logic, reconfiguration slot logic, invitation rules
- [ ] Application unit tests — command handlers with mocked repositories; verify event emission, pricing snapshots, reconfig slot insertion, approval guard
- [ ] Integration tests — real PostgreSQL container; verify DB constraints, RLS enforcement, balance trigger, booking full lifecycle end-to-end

---

> **End-to-end checkpoint:** After Phase 4, a booking should be creatable via the Blazor wizard, persist to Postgres, and push a real-time update back to the calendar. Phases 5–7 are hardening and ops readiness.
