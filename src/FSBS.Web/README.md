# FSBS.Web

Blazor WebAssembly SPA (.NET 10). The browser-side client for both Staff and Customer user populations.

## Responsibilities

- **Routable pages**: one folder per feature under `Pages/` (e.g. `Pages/Bookings/`, `Pages/Simulators/`, `Pages/Account/`)
- **Shared UI components**: calendar, booking wizard, capacity indicator, slot colour legend — under `Components/`
- **Typed HttpClient services**: wrap every API endpoint; all API calls go through here — never raw `HttpClient` in components
- **SignalR client**: connects to `AvailabilityHub`; dispatches Fluxor actions on incoming delta messages
- **Fluxor state slices** under `State/`: `BookingWizardState`, `CalendarState`, `PricingQuoteState`, `SessionState`
- **Layout**: role-adaptive navigation, breadcrumbs, shell — under `Layout/`
- **Polly**: retry + circuit-breaker wrapping all API calls
- **JWT storage**: `Blazored.LocalStorage`

## Packages to add

| Package | Purpose |
|---|---|
| `MudBlazor` | Component library (data grid, dialog, snackbar, form, calendar) |
| `Fluxor` | Redux-style state management |
| `Fluxor.Blazor.Web` | Fluxor Blazor integration + `@inherits FluxorComponent` |
| `Blazored.LocalStorage` | JWT persistence |
| `Microsoft.AspNetCore.Components.WebAssembly.Authentication` | OIDC/Cognito auth flow |
| `Polly` | Retry + circuit-breaker on HttpClient |

## App shell (`Layout/`)

| File | Purpose |
|---|---|
| `MainLayout.razor` | Two-column shell: role-adaptive sidebar + content area; renders `MudLayout` |
| `CustomerLayout.razor` | Minimal shell for customer-facing pages (no staff chrome) |
| `PublicLayout.razor` | No nav — registration, login, invite-claim pages |
| `NavMenu.razor` | Renders different nav items based on `ICurrentUser.AppRole` claim |
| `BreadcrumbBar.razor` | MudBreadcrumbs driven by route metadata |
| `AuthRedirect.razor` | Guard component — redirects unauthenticated users to `/login` |

`App.razor` uses `<AuthorizeRouteView>` with `<CascadingAuthenticationState>`. Unauthenticated users redirect to `/login`.

## Pages

### Public area (no auth — `PublicLayout`)

| Route | File | Notes |
|---|---|---|
| `/` | `Pages/Public/Home.razor` | Marketing landing; links to Login + Register |
| `/login` | `Pages/Public/Login.razor` | Customer Cognito hosted UI redirect |
| `/login/staff` | `Pages/Public/StaffLogin.razor` | Entra ID redirect via Cognito staff pool |
| `/register` | `Pages/Public/Register.razor` | Step 1 of 2: email/password/name form (private customer) |
| `/register/confirm` | `Pages/Public/RegisterConfirm.razor` | Step 2: 6-digit code entry; resend link |
| `/invitations/claim` | `Pages/Public/InvitationClaim.razor` | Token validated via `GET /invitations/validate?token=`; then sign-up form |

### Customer area (`CustomerLayout` — `[Authorize(Policy = "IsCustomer")]`)

**All customer roles:**

| Route | File | Role access | Key features |
|---|---|---|---|
| `/dashboard` | `Pages/Customer/Dashboard.razor` | All customers | Upcoming bookings, quick-book CTA, account balance (corporate only) |
| `/availability` | `Pages/Customer/Availability.razor` | All customers | Simulator picker → calendar; entry point for booking wizard |
| `/availability/{simulatorId}` | `Pages/Customer/SimulatorCalendar.razor` | All customers | Full calendar with slot colour rules; SignalR delta updates |
| `/bookings` | `Pages/Customer/BookingList.razor` | All customers | Paginated list; filter by status; cursor-based |
| `/bookings/{id}` | `Pages/Customer/BookingDetail.razor` | All customers | Status, slots, price, notes, cancel action |
| `/bookings/new` | `Pages/Customer/BookingWizard.razor` | All customers | 7-step (external) / 8-step (InternalStudent) wizard |
| `/profile` | `Pages/Customer/Profile.razor` | All customers | Name, phone, email (read-only from Cognito) |

**Corporate Manager only:**

| Route | File | Notes |
|---|---|---|
| `/organisation` | `Pages/Customer/OrgDashboard.razor` | Account summary, recent bookings, member count |
| `/organisation/account` | `Pages/Customer/OrgAccount.razor` | Balance, payment history table |
| `/organisation/members` | `Pages/Customer/OrgMembers.razor` | Corporate student list; status badges |
| `/organisation/invitations` | `Pages/Customer/OrgInvitations.razor` | Issue new invitation; list pending/claimed/expired |

### Booking wizard steps

| Step | Component | External | InternalStudent |
|---|---|---|---|
| 1 | `WizardStepSimulator.razor` | Pick simulator unit | Same |
| 2 | `WizardStepDate.razor` | Pick date from calendar | Same |
| 3 | `WizardStepSlot.razor` | Pick time slot (shows reconfig windows) | Same |
| 4 | `WizardStepTrainingType.razor` | FlightDeck or CabinCrew | Same |
| 5 | `WizardStepStudents.razor` | Student count + names | Same |
| 6 | `WizardStepInstructor.razor` | Instructor picker (filtered by `TrainingTypeRatings`) | Same |
| 7 | `WizardStepDeptBudget.razor` | — (skipped) | Dept name + budget code (mandatory) |
| 7/8 | `WizardStepConfirm.razor` | Price summary → **Provisional** | Price summary → **PendingApproval** |

Wizard state lives in `BookingWizardState` (Fluxor). Pricing quote hits `GET /pricing/quote` on every step change.

### Staff area (`MainLayout` — `[Authorize(Policy = "IsStaff")]`)

**All staff:**

| Route | File | Notes |
|---|---|---|
| `/staff` | `Pages/Staff/StaffDashboard.razor` | Role-adaptive widgets: approval queue count (SalesStaff), today's sessions (Instructor), open reports (Management) |
| `/staff/schedule` | `Pages/Staff/MasterSchedule.razor` | All bays × all days; heat-map grid; click slot → detail; ScheduleAdmin can drag to reschedule |

**ScheduleAdmin:**

| Route | File | Notes |
|---|---|---|
| `/staff/simulators` | `Pages/Staff/SimulatorList.razor` | All simulator units + status badges |
| `/staff/simulators/{id}` | `Pages/Staff/SimulatorDetail.razor` | Bay list, active config, reconfig templates |
| `/staff/simulators/{id}/configs` | `Pages/Staff/SimulatorConfigs.razor` | Create/edit `SimulatorConfiguration`; supported training types |
| `/staff/simulators/{id}/maintenance` | `Pages/Staff/MaintenanceWindows.razor` | Add/edit maintenance windows; shown on master schedule |
| `/staff/reconfig-templates` | `Pages/Staff/ReconfigTemplates.razor` | CRUD grid; unique `(from, to)` pair enforced in form |
| `/staff/schedule-templates` | `Pages/Staff/ScheduleTemplates.razor` | Repeating schedule patterns per simulator |

**SalesStaff / SystemAdmin:**

| Route | File | Notes |
|---|---|---|
| `/staff/bookings/pending` | `Pages/Staff/PendingApprovals.razor` | Queue of `PendingApproval` bookings; approve/reject inline |
| `/staff/bookings/{id}/review` | `Pages/Staff/BookingReview.razor` | Full booking detail + approve/reject form (reason ≥ 10 chars) |
| `/staff/pricing` | `Pages/Staff/PricingPolicies.razor` | CRUD: base rate per `(config, training type, customer class, effective date)` |
| `/staff/discounts` | `Pages/Staff/DiscountRules.razor` | Rule table; priority ordering via drag-handle; IsCombinable toggle |
| `/staff/organisations` | `Pages/Staff/OrgList.razor` | Paginated org table; link to detail |
| `/staff/organisations/{id}` | `Pages/Staff/OrgDetail.razor` | Org info, account balance, member list |
| `/staff/organisations/{id}/payments` | `Pages/Staff/OrgPayments.razor` | Record new payment; verify/void existing; method + amount + ref |
| `/staff/invitations/corporate` | `Pages/Staff/IssueCorporateInvite.razor` | Issue CorporateManager invite; scoped to org |

**CourseDirector:**

| Route | File | Notes |
|---|---|---|
| `/staff/courses` | `Pages/Staff/CourseList.razor` | All courses; filter by training type |
| `/staff/courses/{id}` | `Pages/Staff/CourseDetail.razor` | Modules → Lessons tree; enrolment count |
| `/staff/enrolments` | `Pages/Staff/EnrolmentList.razor` | All student enrolments; filter by status |
| `/staff/enrolments/{id}` | `Pages/Staff/EnrolmentDetail.razor` | Progress records; sign-off buttons per lesson |

**Instructor:**

| Route | File | Notes |
|---|---|---|
| `/staff/my-schedule` | `Pages/Staff/InstructorSchedule.razor` | Own upcoming slots calendar; assigned student list per session |
| `/staff/availability` | `Pages/Staff/InstructorAvailability.razor` | Set own available/leave/other windows |

**Management:**

| Route | File | Notes |
|---|---|---|
| `/staff/reports` | `Pages/Staff/ReportList.razor` | Report definitions table; trigger run |
| `/staff/reports/{id}` | `Pages/Staff/ReportDetail.razor` | Run status; download/view completed run output |
| `/staff/analytics` | `Pages/Staff/Analytics.razor` | Revenue + utilisation charts; uses `fsbs_readonly` role data |

**SystemAdmin:**

| Route | File | Notes |
|---|---|---|
| `/staff/users` | `Pages/Staff/UserList.razor` | All `AppUsers`; soft-delete; role badge |
| `/staff/qualifications` | `Pages/Staff/Qualifications.razor` | Instructor qualification records; expiry warnings |

**InternalStudent** (staff pool, uses customer booking flow):

| Route | File | Notes |
|---|---|---|
| `/staff/my-bookings` | `Pages/Staff/InternalStudentBookings.razor` | Own bookings; PendingApproval badge prominent |

InternalStudent uses the same `BookingWizard` at `/bookings/new` — the wizard detects the role from auth state and renders the 8-step variant.

## Shared components (`Components/`)

| Component | Purpose |
|---|---|
| `AvailabilityCalendar.razor` | Week/day grid; colour-coded slots per slot colour rules; SignalR real-time updates |
| `CapacityIndicator.razor` | Amber/green pill showing remaining seats |
| `BookingStatusBadge.razor` | Coloured chip for each `BookingStatus` enum value |
| `PricingQuoteSummary.razor` | Live pricing summary panel used in wizard + detail pages |
| `ConfirmDialog.razor` | MudDialog wrapper — reused for approve/reject/cancel/void actions |
| `PaginatedTable.razor` | Generic cursor-based paginated MudDataGrid wrapper |
| `InvitationStatusBadge.razor` | Pending/Claimed/Expired/Revoked chip |
| `PaymentMethodIcon.razor` | Icon + label for each `PaymentMethod` enum |

## Availability calendar slot colours

| State | Colour | Behaviour |
|---|---|---|
| Available (>25% capacity) | Green | Selectable |
| Available (<25% capacity) | Amber | Selectable with tooltip |
| Reconfiguration window | Grey hatched | Non-selectable; tooltip shows transition + duration |
| Maintenance | Dark grey | Non-selectable |
| Booked by this user | Blue | Links to booking detail |
| Full | Red | Non-selectable |

## Fluxor state slices (`State/`)

| Slice | Key state | Key actions |
|---|---|---|
| `BookingWizardState` | `CurrentStep`, `SelectedSimulatorId`, `SelectedDate`, `SelectedSlot`, `TrainingType`, `StudentCount`, `InstructorId`, `DeptName`, `BudgetCode`, `PriceQuote` | `SetStep`, `SetSimulator`, `SetSlot`, `SetQuote`, `ResetWizard` |
| `CalendarState` | `SimulatorId`, `WeekStart`, `AvailabilityGrid`, `ReconfigWindows`, `MaintenanceWindows` | `LoadGrid`, `ApplyDelta`, `SetWeek` |
| `SessionState` | `UserId`, `TenantId`, `AppRole`, `OrgId`, `IsAuthenticated` | `SetSession`, `ClearSession` |
| `PendingApprovalsState` | `Items[]`, `IsLoading`, `LastCursor` | `Load`, `Approve`, `Reject` |

## Typed HttpClient services (`Services/`)

| Service | API calls covered |
|---|---|
| `AuthService` | `POST /v1/auth/register`, confirm, resend-code |
| `AvailabilityService` | `GET /v1/simulators/{id}/availability` |
| `PricingService` | `GET /v1/pricing/quote` |
| `BookingService` | `POST /v1/bookings`, list, detail, approve, reject, cancel |
| `InvitationService` | `GET /v1/invitations/validate`, `POST /v1/invitations` |
| `OrganisationService` | Org CRUD, payments endpoints |
| `SimulatorService` | Simulator + config + maintenance + reconfig template endpoints |
| `CourseService` | Course + module + lesson + enrolment + progress endpoints |
| `ReportService` | Report definition + run endpoints |
| `AvailabilityHubClient` | SignalR `AvailabilityHub` connection; dispatches `ApplyDelta` Fluxor action on message |

All services wrap calls with a Polly `ResiliencePipeline` — retry × 3 with exponential backoff, circuit-break on 5 failures in 30 s.

## Booking wizard step counts

| User type | Steps | Notes |
|---|---|---|
| External customer | 7 | Ends Provisional → customer confirms → Confirmed |
| Internal student | 8 | Step 7 = Department + Budget Code (both mandatory); ends PendingApproval |

## Implementation order

1. Add packages to csproj; configure MudBlazor + Fluxor + Blazored.LocalStorage in `Program.cs`
2. Replace `MainLayout` / `NavMenu`; add `CustomerLayout` + `PublicLayout`; wire `App.razor` with `AuthorizeRouteView`
3. Create Fluxor state slices + actions + reducers (no HTTP yet)
4. Create typed HttpClient services (stubs returning empty data)
5. Public pages: `Home`, `Login`, `StaffLogin`, `Register`, `RegisterConfirm`, `InvitationClaim`
6. Shared components: `BookingStatusBadge`, `CapacityIndicator`, `ConfirmDialog`, `PaginatedTable`
7. Customer pages: `Dashboard`, `BookingList`, `BookingDetail`, `Profile`
8. `AvailabilityCalendar` component + `SimulatorCalendar` page + SignalR hub client
9. `BookingWizard` + all step sub-components + `PricingQuoteSummary`
10. Corporate Manager pages: `OrgDashboard`, `OrgAccount`, `OrgMembers`, `OrgInvitations`
11. Staff shell + `StaffDashboard` + `MasterSchedule`
12. ScheduleAdmin pages
13. SalesStaff pages (approval queue first — high business value)
14. CourseDirector + Instructor + Management + SystemAdmin pages

## Do not add

- Business logic or domain rules (call API services instead)
- Direct HTTP calls outside of `Services/` typed clients
- Domain entities from `FSBS.Domain` (use DTOs from `FSBS.Shared`)
