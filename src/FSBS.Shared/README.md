# FSBS.Shared

Shared contract library referenced by both `FSBS.Api` and `FSBS.Web`. Keeps the API surface and client in sync without duplicating types.

## Responsibilities

- **DTOs**: request and response shapes for every API endpoint (e.g. `CreateBookingRequest`, `BookingDto`, `PricingQuoteDto`, `SimulatorAvailabilityDto`)
- **Enums**: `AppRole`, `BookingStatus`, `TrainingType`, `ConfigMode`, `InvitationStatus`, `SlotColour`, etc.
- **FluentValidation rules**: validators that are meaningful to validate client-side as well as server-side (e.g. `CreateBookingRequestValidator`)

## Do not add

- Domain entities or aggregate roots (those belong in `FSBS.Domain`)
- EF Core types, MediatR types, or infrastructure concerns
- Blazor or ASP.NET Core dependencies
- Business logic beyond simple field validation
