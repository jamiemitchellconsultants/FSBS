# FSBS.Infrastructure.Persistence.Repositories

Concrete repository implementations that fulfil the contracts defined in `FSBS.Infrastructure.Persistence.Repositories.Interfaces`. Registered with the DI container via `RepositoriesServiceExtensions.AddRepositories`, which is called from `FSBS.Api/Program.cs`.

## Responsibilities

- Implement each `IRepository<T>` interface from `FSBS.Infrastructure.Persistence.Repositories.Interfaces`
- Use `FsbsDbContext` for write operations and simple EF queries
- Use `IDbConnection` (Dapper) for complex read queries that benefit from raw SQL — joined projections, pagination cursors, availability grid calculations
- Keep all database access out of Application handlers; handlers depend only on repository interfaces, never on EF types directly

## When to use EF vs Dapper

| Scenario | Use |
|---|---|
| Inserts, updates, deletes | EF Core via `FsbsDbContext` |
| Simple lookups by PK or FK | EF Core |
| Multi-join projections, cursor pagination, availability grids | Dapper via `IDbConnection` |
| Aggregations for reporting | Dapper |

## Registration

```csharp
// FSBS.Api/Program.cs
builder.Services.AddRepositories();
```

`AddRepositories` is the only public surface of this project from the API's perspective. All repository interfaces are consumed by Application handlers through the interfaces project.

## Dependencies

```
FSBS.Application
FSBS.Infrastructure.Persistence
FSBS.Infrastructure.Persistence.Repositories.Interfaces
```

## Do not add

- `IEntityTypeConfiguration<T>` files or EF model configuration
- Business logic or domain rules — those belong in Domain and Application
- MediatR handlers or pipeline behaviours
- Direct references to AWS SDKs (use Application service interfaces instead)
