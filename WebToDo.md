# Blazor UI Build Order

## Phase 1 — Auth Shell (unblock everything else) ✅
The `AnonymousAuthStateProvider` is a stub — nothing role-adaptive works until this is real. Build this first so every subsequent page can be tested with actual identity.

1. [x] Replace `AnonymousAuthStateProvider` with a real Cognito OIDC provider (PKCE flow for customers, redirect to `/staff-login` for Entra)
2. [x] Populate `SessionState` (UserId, AppRole, OrgId) on login so the Fluxor store has identity context
3. [x] Wire `/login`, `/register`, `/register/confirm`, `/invitation/claim` pages — these already exist as scaffolding in `Pages/Public/`

---

## Phase 2 — Availability Calendar (core entry point) ✅
`AvailabilityMonth.razor` is the most complete page and is the natural landing page after login. It's ~90% done.

4. [x] Verify month → week drill-down against live API
5. [x] Add capacity indicator tooltip on month cells (reconfiguration window overlay is noted as missing in ToDo)
6. [x] Confirm SignalR push via `AvailabilityHubClient` refreshes the calendar without a full reload

---

## Phase 3 — Booking Wizard (primary customer action)
`BookingWizard.razor` is well-built. The gap is end-to-end verification.

7. Verify wizard submits to live API and navigates to `BookingDetail`
8. Build `BookingDetail.razor` (currently empty) — show status, slot, price, cancel button
9. Build `MyBookings.razor` — the list view with cursor-based pagination (state slice exists in `MyBookingsState`)

---

## Phase 4 — Staff Core (schedule + approvals)
These are the highest-value staff pages with no scaffolding yet.

10. `/staff` dashboard — summary cards (pending approvals count, today's bookings)
11. `/staff/bookings/pending` — pending approvals list with approve/reject actions (state slice exists in `PendingApprovalsState`)
12. `/staff/schedule` — master schedule week view (reuse the week view logic from `AvailabilityMonth`)

---

## Phase 5 — Organisation & Invitations

13. `/organisation` overview, `/organisation/members`, `/organisation/invitations` — `OrgInvitations.razor` exists, needs binding
14. `/staff/invitations/corporate` — `IssueCorporateInvitation.razor` exists, needs binding
15. `/staff/organisations` — list + detail for corporate accounts

---

## Phase 6 — Staff Admin (lower traffic, lower risk)

16. Simulators CRUD (`/staff/simulators`, reconfig templates, schedule templates)
17. Pricing & discounts (`/staff/pricing`, `/staff/discounts`)
18. Courses & enrolments (`/staff/courses`, `/staff/enrolments`)
19. Instructor schedule & availability (`/staff/my-schedule`, `/staff/availability`)
20. Reports & analytics (`/staff/reports`, `/staff/analytics`)

---

## Key principle
The dependency chain is: **Auth → Calendar → Booking Wizard → BookingDetail/MyBookings → Staff approvals**. Each phase unblocks the next. Don't build staff admin pages until the customer booking loop is verified end-to-end, since that's the critical path for the business.
