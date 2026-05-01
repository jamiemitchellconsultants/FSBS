# FSBS.Api

ASP.NET Core 10 minimal API — the HTTP and real-time boundary of the system. Translates HTTP requests into MediatR commands/queries and streams availability updates via SignalR.

## Responsibilities

- **Minimal API endpoints**: one file per feature group under `Endpoints/`
- **SignalR hub** (`AvailabilityHub`): pushes slot-availability delta messages to connected clients on booking create/modify/cancel; uses Redis (ElastiCache) as the backplane
- **JWT authentication**: two schemes — `Staff` (Cognito Staff Pool) and `Customer` (Cognito Customer Pool)
- **Claims transformation** (`FsbsClaimsTransformation`): maps `app_role` and `tenant_id` from either pool into a unified `FsbsPrincipal`
- **Authorization policies**: named policies (`RequireSalesStaff`, `RequireSystemAdmin`, etc.) mapped from `AppRole` enum in `Program.cs`
- **Middleware**: tenant resolution, idempotency key validation, Problem Details error handling (RFC 7807)
- **`Idempotency-Key` header**: required on `POST /bookings`; enforced in middleware

## Folder structure

```
Auth/          FsbsClaimsTransformation, policy registration
Endpoints/     One static class per feature (BookingsEndpoints, SimulatorsEndpoints, etc.)
Hubs/          AvailabilityHub
Middleware/    TenantMiddleware, IdempotencyMiddleware, ExceptionMiddleware
```

## API conventions

- Base URL: `https://api.fsbs.example.com/v1`
- Versioning: URL prefix (`/v1/`, `/v2/`)
- Pagination: cursor-based (`?after=<cursor>&limit=<n>`) — never offset
- Errors: Problem Details (`application/problem+json`)

## Do not add

- Business logic or domain rules (delegate everything to MediatR)
- Direct EF Core or repository calls
- AWS SDK calls (use Application interfaces)
