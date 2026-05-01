# FSBS.Infrastructure

Implements the interfaces defined in Application. All I/O — database, cache, messaging, email, file storage — lives here.

## Responsibilities

- **EF Core DbContext** (`FsbsDbContext`): entity configurations, global query filters (soft delete, multi-tenancy), audit interceptor, `xmin`-based optimistic concurrency
- **EF Core migrations**: code-first; applied by CI/CD only — never run `dotnet ef database update` manually against production
- **Dapper read models**: complex join queries that bypass EF, injected via `IDbConnection`
- **Repository implementations**: implement `IBookingRepository`, `ISimulatorRepository`, etc. from Domain
- **Availability cache** (`AvailabilityCache`): Redis via ElastiCache; 60-second TTL; invalidated on every booking mutation
- **Notification adapters**: SQS publisher for domain events; SES email client
- **S3 adapter**: pre-signed URL generation for `fsbs-documents` bucket (never served through CloudFront)
- **Cognito client**: used by invitation and user-management flows

## Key conventions

- snake_case column names via `EFCore.NamingConventions`
- UUIDs as PKs (`uuid_generate_v4()` default)
- `timestamptz` / `DateTimeOffset` for all timestamps
- `is_deleted` boolean + global query filter for soft deletes
- `created_at`, `updated_at`, `created_by`, `updated_by` on every table via EF interceptor
- Multi-tenancy: `tenant_id` injected from JWT into DbContext before any handler runs

## Do not add

- Business logic or domain rules
- MediatR handlers (those belong in Application)
- ASP.NET Core middleware or endpoint routing
- Blazor components or services
