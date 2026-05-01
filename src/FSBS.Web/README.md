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

## Booking wizard step counts

| User type | Steps | Notes |
|---|---|---|
| External customer | 7 | Ends Provisional → customer confirms → Confirmed |
| Internal student | 8 | Step 7 = Department + Budget Code (both mandatory); ends PendingApproval |

## Availability calendar slot colours

| State | Colour |
|---|---|
| Available (>25% capacity) | Green |
| Available (<25% capacity) | Amber |
| Reconfiguration window | Grey hatched (non-selectable) |
| Maintenance | Dark grey (non-selectable) |
| Booked by this user | Blue |
| Full | Red (non-selectable) |

## Do not add

- Business logic or domain rules (call API services instead)
- Direct HTTP calls outside of `Services/` typed clients
- Domain entities from `FSBS.Domain` (use DTOs from `FSBS.Shared`)
