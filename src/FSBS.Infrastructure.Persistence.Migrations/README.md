# FSBS.Infrastructure.Persistence.Migrations

Owns the EF Core migration history for FSBS. This project is self-contained for design-time tooling — both `-p` (project) and `-s` (startup project) point here when running `dotnet ef` commands, because `FsbsDbContextFactory` provides everything the tools need without involving the API project.

## Responsibilities

- **`FsbsDbContextFactory`**: implements `IDesignTimeDbContextFactory<FsbsDbContext>` so that `dotnet ef` can construct the context at design time without a running application host. Uses a hardcoded local connection string and a stub `ICurrentUser` (`UserId = Guid.Empty`, `TenantId = Guid.Empty`) — the stub is only ever exercised during migration generation, never at runtime
- **`Migrations/` folder**: generated and managed exclusively by `dotnet ef migrations add / remove / script`; never edited by hand

## Running migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> \
  -p src/FSBS.Infrastructure.Persistence.Migrations \
  -s src/FSBS.Infrastructure.Persistence.Migrations

# List applied migrations (connection refused is acceptable if no local DB)
dotnet ef migrations list \
  -p src/FSBS.Infrastructure.Persistence.Migrations \
  -s src/FSBS.Infrastructure.Persistence.Migrations

# Generate a SQL script for production deployment
dotnet ef migrations script \
  -p src/FSBS.Infrastructure.Persistence.Migrations \
  -s src/FSBS.Infrastructure.Persistence.Migrations \
  -o migration.sql
```

**Never run `dotnet ef database update` against production.** Migrations are applied exclusively by the CI/CD pipeline using the `fsbs_migrations` database role, which bypasses row-level security. The application role (`fsbs_app`) does not have DDL permissions.

## Dependencies

```
FSBS.Infrastructure.Persistence
Microsoft.EntityFrameworkCore.Design 10.0.7  (PrivateAssets=all — not propagated to consumers)
```

`Microsoft.EntityFrameworkCore.Design` is kept out of `FSBS.Infrastructure.Persistence` to prevent accidental design-time tool invocations against that project.

## Do not add

- Entity classes or `IEntityTypeConfiguration<T>` files
- Repository implementations or business logic
- Any runtime-registered services — this project is design-time only
