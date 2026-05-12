# FSBS Blazor WASM — Implemented UI Features

Based on `src/FSBS.Web/`, organized by area.

## Foundations
- **Layouts**: `PublicLayout`, `MainLayout`, `CustomerLayout`; role-adaptive `NavMenu.razor:1` that switches sections based on JWT roles.
- **Auth providers**: `CognitoAuthStateProvider`, `AnonymousAuthStateProvider`, plus `BookingAccessEvaluator` for client-side gating.
- **State (Fluxor slices)**: `BookingWizard`, `Calendar`, `InstructorSchedule`, `MyBookings`, `PendingApprovals`, `Session`.
- **Typed services** (`Services/*.cs`): `AuthService`, `BookingService`, `AvailabilityService`, `PricingService`, `SimulatorService`, `AircraftTypeService`, `InstructorScheduleService`, `OrganisationService`, `InvitationService`, `CourseService`, `ReferenceDataService`, `ReportService`, `UserProfileService`, plus `AvailabilityHubClient` (SignalR).

## Public / Auth
- `/` Home, `/login`, `/login/staff` (Entra), `/logout`, `/auth/callback`, `/register`, `/register/confirm`, `/invitations/claim`, `/dev/login`, `/not-found`.

## Availability & Booking (customers + internal students)
- `/availability` — month grid with **live SignalR updates** via `AvailabilityHubClient.StartAsync("/hubs/availability")` and per-simulator subscriptions (`AvailabilityMonth.razor:494`, `:514`).
- `/bookings/new` — **multi-step booking wizard** (`BookingWizard.razor`): Step 1 Simulator/Bay/Slot → 2 Training Details → 3 Department & Budget *(internal students only)* → Pricing quote → Confirm. Persists across steps via Fluxor; surfaces 4-hour minimum and "Pending Approval" outcome for internal students.
- `/my-bookings`, `/bookings/{id:guid}` — list and detail views.
- `Bookings/Components/` — `BookingsCalendarView`, `BookingsDayView`, `BookingsWeekView`, `BookingsListView`.

## Instructor
- `/instructor/schedule` — working-times editor with month/week views, single-day and standard-week dialogs, navigation bar, and legend (`Components/InstructorSchedule/*`).

## Staff
- **Bookings**: `/staff/bookings/pending` — approval queue (`PendingApprovals.razor`).
- **Simulators** (`/staff/simulators`, `/staff/aircraft-types`): CRUD dialogs for `SimulatorUnit`, `SimulatorBay`, `SimulatorConfig`, `AircraftType`.
- **Instructor schedules**: `/staff/instructors/schedules` list + `/staff/instructors/{InstructorId}/schedule` editor.
- **Organisations**: `/staff/organisations/payments` — `RecordPayment.razor` (creates Pending payments).
- **Invitations**: `/staff/invitations/corporate` — issue CorporateManager invitations.
- **Reference data**: `/staff/reference-data/customer-classes`, `/discount-types`, `/payment-methods`, `/account-statuses` — generic list/dialog pattern (`ReferenceItemList`, `ReferenceItemDialog`, plus `AccountStatusList`/`AccountStatusDialog`).

## Corporate Manager (customer org admin)
- `/organisation/invitations` — `OrgInvitations.razor` for inviting CorporateStudents within the manager's own org.

## Account
- `/account/profile` — user profile management.

## What's in the nav but not yet routed
`NavMenu.razor` exposes links that don't have matching `@page` routes yet: `/dashboard`, `/staff`, `/staff/schedule`, `/staff/reconfig-templates`, `/staff/schedule-templates`, `/staff/pricing`, `/staff/discounts`, `/staff/organisations`, `/staff/courses`, `/staff/enrolments`, `/staff/my-schedule`, `/staff/reports`, `/staff/analytics`, `/staff/users`, `/staff/qualifications`, `/staff/my-bookings`, `/organisation`, `/organisation/account`, `/organisation/members`. These are stubbed navigation slots — pages aren't implemented yet.
