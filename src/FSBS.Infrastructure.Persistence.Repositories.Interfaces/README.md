# FSBS.Infrastructure.Persistence.Repositories.Interfaces

Defines the repository contracts that Application handlers depend on. By living in its own project this assembly can be referenced by both `FSBS.Application` (consumers) and `FSBS.Infrastructure.Persistence.Repositories` (implementations) without creating a circular dependency.

## Responsibilities

- Declare one interface per aggregate root or significant query boundary (e.g. `IBookingRepository`, `ISimulatorRepository`, `IOrganisationRepository`)
- Keep interfaces focused on the use cases that exist — no speculative generic CRUD surface
- Expose domain entity types and primitive return values only; no EF or Dapper types leak through

## Dependency direction

```
FSBS.Application
  └── depends on  FSBS.Infrastructure.Persistence.Repositories.Interfaces
                        └── depends on  FSBS.Domain

FSBS.Infrastructure.Persistence.Repositories
  └── implements  FSBS.Infrastructure.Persistence.Repositories.Interfaces
```

This arrangement means Application handlers are entirely decoupled from EF Core, Npgsql, and PostgreSQL. Swapping the persistence implementation requires only a change in the Repositories project and the DI registration.

## Adding a new interface

1. Add the interface file here (e.g. `IInvoiceRepository.cs`)
2. Implement it in `FSBS.Infrastructure.Persistence.Repositories`
3. Register the implementation in `RepositoriesServiceExtensions.AddRepositories`
4. Inject via the interface in the relevant Application handler

## Dependencies

```
FSBS.Domain
```

## Do not add

- Concrete implementations — those belong in `FSBS.Infrastructure.Persistence.Repositories`
- EF Core, Dapper, Npgsql, or any I/O reference
- DTOs or Application-layer types — interfaces operate on domain entities and primitives only
- Generic base repository interfaces that add surface area beyond what handlers actually need
