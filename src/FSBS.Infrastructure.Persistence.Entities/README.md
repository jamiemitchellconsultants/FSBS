# FSBS.Infrastructure.Persistence.Entities

Contains every `IEntityTypeConfiguration<T>` for the 34 FSBS domain entities. This project exists as a separate assembly so that `FsbsDbContext` can call `ApplyConfigurationsFromAssembly` without the configuration code living inside the core persistence project, keeping Fluent API concerns cleanly separated from `DbContext` lifecycle concerns.

## Responsibilities

- **34 configuration files** in `Configurations/`, one per entity, each implementing `IEntityTypeConfiguration<T>`
- Declares primary key column names, property constraints, enum-to-string conversions, array column types, and check constraints
- Configures all foreign key relationships and navigation properties
- Registers `xmin`-based optimistic concurrency tokens on every `AuditableEntity` table

## Conventions applied in every configuration

| Pattern | Example |
|---|---|
| PK column name | `builder.Property(e => e.Id).HasColumnName("booking_id")` |
| Optimistic concurrency | `builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()` |
| Enum stored as string | `builder.Property(e => e.Status).HasConversion<string>().IsRequired()` |
| PostgreSQL array | `builder.Property(e => e.SupportedTrainingTypes).HasColumnType("training_type[]")` |
| jsonb | `builder.Property(e => e.DefinitionJson).HasColumnType("jsonb")` |
| Check constraint | `builder.HasCheckConstraint("ck_name", "sql expression")` |
| Filtered unique index | `builder.HasIndex(...).HasFilter("status = 'Pending'").IsUnique()` |

## Notable special cases

| Entity | Why it is special |
|---|---|
| `UserProfile` | Shares the `user_id` PK column with `app_users`; no separate FK column |
| `OrgAccount` | `current_balance_gbp` is trigger-maintained; configured as `ValueGeneratedOnAddOrUpdate` + `PropertySaveBehavior.Ignore` |
| `BookingDiscount` | Immutable; no `xmin` token; `CreatedAt` marked `PropertySaveBehavior.Ignore` after insert |
| `SimulatorConfiguration` | `supported_training_types` is a `training_type[]` PostgreSQL array |
| `Instructor` | `training_type_ratings` is a `training_type[]` PostgreSQL array |
| `Report` | `definition_json` stored as `jsonb` |

## Dependencies

```
FSBS.Domain
Microsoft.EntityFrameworkCore 10.0.7
Microsoft.EntityFrameworkCore.Relational 10.0.7
```

`CS0618` is suppressed project-wide because `HasCheckConstraint(string, string)` is marked obsolete in EF Core 10 but remains the correct API until a non-obsolete replacement is available.

## Do not add

- `DbContext` or interceptors (those belong in `FSBS.Infrastructure.Persistence`)
- Repository implementations or interfaces
- Business logic or domain rules
- Any reference to `Npgsql`, ASP.NET Core, or AWS SDKs
