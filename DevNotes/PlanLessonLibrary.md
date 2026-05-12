# Plan — Lesson Library (reusable lesson templates)

## Context

Today `Lesson` is strictly module-owned (`Lesson.ModuleId` FK; unique index on `(module_id, sequence_order)`). There is no way to author a lesson once and reuse it across courses/modules. CourseDirectors end up retyping the same training content into every new Module.

This plan adds a **tenant-scoped library of reusable lesson templates**. The library is a separate aggregate — `LessonTemplate` — that holds the "blueprint" for a lesson (title, description, default duration, instructor requirement, etc.). When a CourseDirector adds a template to a Module, the template's fields are **copied** into a new `Lesson` row. Editing a `LessonTemplate` afterwards does **not** mutate previously-attached `Lesson` rows (matches FSBS's price-immutability / discount-snapshot convention).

**Decisions confirmed with user**
- **Model**: Templates + instances. New `LessonTemplate` aggregate; existing `Lesson` unchanged in shape (one optional FK added for provenance — see below).
- **Write roles**: `SystemAdmin`, `CourseDirector`. (Management is **read-only**, matching the role table in `CLAUDE.md`.)
- **Read roles**: `SystemAdmin`, `CourseDirector`, `Management`, `SalesStaff`, `Instructor` (= "Trainer").
- **Role mapping**: "Manager" → `Management`; "Trainer" → `Instructor`.

## Data model

### New aggregate: `LessonTemplate`

`src/FSBS.Domain/Entities/LessonTemplate.cs`

```csharp
public class LessonTemplate : AuditableEntity, ISoftDeletable, ITenantScoped
{
    public Guid TenantId { get; set; }

    /// Display title shown in the library list and copied into Lesson.Title on attach.
    public string Title { get; set; } = string.Empty;

    /// Optional learning objectives / content summary.
    public string? Description { get; set; }

    /// FlightDeck or CabinCrew — gates which course types can pull this template
    /// (must match the Course.TrainingType when attaching to a Module).
    public TrainingType TrainingType { get; set; }

    /// Suggested minimum duration in minutes — copied into Lesson.MinDurationMins on attach.
    public int DefaultMinDurationMins { get; set; }

    /// Default value copied into Lesson.RequiresInstructor on attach.
    public bool RequiresInstructor { get; set; } = true;

    /// Default value copied into Lesson.IsMandatory on attach.
    public bool IsMandatoryByDefault { get; set; } = true;

    /// Free-text categorisation, e.g. "Emergencies", "Navigation". Optional; used for filters.
    public string? Category { get; set; }

    /// Library-visibility flag. False = retired but historical attachments retained.
    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }
}
```

### EF configuration: `LessonTemplateConfiguration.cs`

Mirror `LessonConfiguration` + tenancy + Course-style constraints:

- Table: `lesson_templates` (snake_case via existing naming convention).
- `Title` `varchar(300)` NOT NULL.
- `Description` `text` nullable.
- `Category` `varchar(100)` nullable.
- `DefaultMinDurationMins` int NOT NULL, `CHECK (default_min_duration_mins > 0)`.
- `TrainingType` native PG enum `fsbs.training_type` (existing).
- `IsActive` bool NOT NULL default `true`.
- `IsDeleted` bool NOT NULL default `false`.
- `xmin` concurrency token.
- **Unique partial index**: `(tenant_id, lower(title)) WHERE is_deleted = false` — prevents duplicate library entries per tenant.
- Filterable index: `(tenant_id, training_type, is_active, is_deleted)` for the list query.
- Tenant query filter (mirrors other tenant-scoped entities) — `builder.HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantId)`.

### Minor change to `Lesson` — provenance

Add an optional FK so each module-owned `Lesson` can trace back to its source template (purely informational; nullable for lessons not authored from the library).

`src/FSBS.Domain/Entities/Lesson.cs` — add:
```csharp
public Guid? SourceTemplateId { get; set; }
public LessonTemplate? SourceTemplate { get; set; }
```

`LessonConfiguration.cs` — add an unenforced `HasOne(l => l.SourceTemplate).WithMany().HasForeignKey(l => l.SourceTemplateId).OnDelete(DeleteBehavior.SetNull)`. No unique constraint (one template can be attached to many lessons).

### Migration

`AddLessonLibrary`:
1. `CREATE TABLE fsbs.lesson_templates (...)` with constraints + indexes above.
2. `ALTER TABLE fsbs.lessons ADD COLUMN source_template_id uuid NULL REFERENCES fsbs.lesson_templates(lesson_template_id) ON DELETE SET NULL;`
3. RLS policy on `lesson_templates` matching the existing pattern (`tenant_id = current_setting('app.current_tenant_id')::uuid`).
4. Grants: `fsbs_app` → SELECT/INSERT/UPDATE/DELETE; `fsbs_readonly` → SELECT.

## Authorisation

Add two new policies in `src/FSBS.Api/Program.cs`:

```csharp
.AddPolicy("RequireLessonLibraryWriter", p => p.RequireAssertion(ctx =>
    ctx.User.HasClaim("app_role", "SystemAdmin") ||
    ctx.User.HasClaim("app_role", "CourseDirector")))

.AddPolicy("RequireLessonLibraryReader", p => p.RequireAssertion(ctx =>
    ctx.User.HasClaim("app_role", "SystemAdmin") ||
    ctx.User.HasClaim("app_role", "CourseDirector") ||
    ctx.User.HasClaim("app_role", "Management") ||
    ctx.User.HasClaim("app_role", "SalesStaff") ||
    ctx.User.HasClaim("app_role", "Instructor")))
```

## Application layer (CQRS)

Folder: `src/FSBS.Application/LessonLibrary/`. Mirrors the Invitation pattern verified in the previous plan.

### Commands

1. **`CreateLessonTemplateCommand`** → `CreateLessonTemplateResult`
   - Fields: `Title`, `Description?`, `TrainingType`, `DefaultMinDurationMins`, `RequiresInstructor`, `IsMandatoryByDefault`, `Category?`.
   - Validator: NotEmpty Title (≤300), Description ≤4000, Category ≤100, DefaultMinDurationMins ∈ [1, 1440], `TrainingType` `IsInEnum`.
   - Handler: build entity with `TenantId = currentUser.TenantId`; call `lessonTemplates.AddAsync`.

2. **`UpdateLessonTemplateCommand`** → returns updated DTO.
   - Fields: `Id` + all editable fields (same as Create).
   - Handler: load by id, mutate, persist. 404 if not found, 409 on concurrency (xmin).

3. **`SetLessonTemplateActiveCommand`** → returns updated DTO.
   - Fields: `Id`, `IsActive`.
   - Cheap toggle for retire / unretire without rewriting fields.

4. **`SoftDeleteLessonTemplateCommand`** → returns void.
   - Sets `IsDeleted = true`. Does NOT cascade — existing attached `Lesson` rows remain intact (templates are a copy source, not a live link). Modules that already pulled fields retain them.
   - Optionally check usage count and surface it in the response (informational only).

5. **`AttachLessonTemplateToModuleCommand`** → returns `LessonDto`.
   - Fields: `LessonTemplateId`, `ModuleId`, `SequenceOrder`, optional overrides `MinDurationMins?`, `RequiresInstructor?`, `IsMandatory?`.
   - Handler:
     1. Load template; reject if `IsDeleted` or `IsActive == false`.
     2. Load parent module → load parent course → assert `course.TrainingType == template.TrainingType` (capability gate).
     3. Build new `Lesson` copying template fields (applying overrides); set `SourceTemplateId = template.Id`, `ModuleId`, `SequenceOrder`.
     4. Insert via `ILessonRepository.AddAsync` (new — see Repository section).
     5. Returns `LessonDto`.
   - Validator: `SequenceOrder >= 1`; overrides where present obey same ranges as the template fields.
   - Will surface DB unique violation `(module_id, sequence_order)` as `409 Conflict` with a clear message.

### Queries

1. **`ListLessonTemplatesQuery`** → cursor-paginated `LessonTemplateListItemDto`.
   - Filters: `trainingType?`, `category?`, `isActive?` (default true), `search?` (case-insensitive title prefix).
   - Cursor-based pagination per FSBS convention (`after`, `limit`).

2. **`GetLessonTemplateByIdQuery`** → `LessonTemplateDto` (full detail) + `usageCount` (sum of attached `Lessons` referencing this template).

## Repository

### New: `src/FSBS.Domain/Interfaces/ILessonTemplateRepository.cs`

```csharp
public interface ILessonTemplateRepository
{
    Task AddAsync(LessonTemplate template, CancellationToken ct = default);
    Task<LessonTemplate?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(LessonTemplate template, CancellationToken ct = default);
    Task<int> CountAttachedLessonsAsync(Guid templateId, CancellationToken ct = default);
}
```

### New: `src/FSBS.Domain/Interfaces/ILessonRepository.cs`

(`Lesson` doesn't yet have a write repo — needed for `AttachLessonTemplateToModuleCommand`.)

```csharp
public interface ILessonRepository
{
    Task AddAsync(Lesson lesson, CancellationToken ct = default);
}
```

### Implementations

- `src/FSBS.Infrastructure.Persistence.Repositories/LessonTemplateRepository.cs`
- `src/FSBS.Infrastructure.Persistence.Repositories/LessonRepository.cs`

Register both in `RepositoriesServiceExtensions.AddRepositories`.

### Read-side projection

`src/FSBS.Infrastructure.Persistence.Repositories/LessonTemplateReadRepository.cs` implementing `ILessonTemplateReadRepository` (new interface in `FSBS.Infrastructure.Persistence.Repositories.Interfaces`) for cursor-paginated DTO projection used by `ListLessonTemplatesQuery`. Use Dapper if the query needs `usageCount` joins beyond EF's natural projection; EF + `Select` is fine for the basic list.

## API layer

### New: `src/FSBS.Shared/LessonLibrary/` DTOs

- `LessonTemplateDto` — full detail + `UsageCount`.
- `LessonTemplateListItemDto` — list-row shape (id, title, training type, category, isActive, usageCount).
- Request types: `CreateLessonTemplateRequest`, `UpdateLessonTemplateRequest`, `SetActiveRequest`, `AttachLessonToModuleRequest`.
- Response types: `LessonTemplateResponse` (same as DTO), `AttachLessonResponse` (wraps `LessonDto`).

### New: `src/FSBS.Api/Endpoints/LessonTemplateEndpoints.cs`

```
POST   /v1/lesson-templates                     RequireLessonLibraryWriter   → CreateLessonTemplate
GET    /v1/lesson-templates                     RequireLessonLibraryReader   → ListLessonTemplates
GET    /v1/lesson-templates/{id}                RequireLessonLibraryReader   → GetLessonTemplateById
PUT    /v1/lesson-templates/{id}                RequireLessonLibraryWriter   → UpdateLessonTemplate
PUT    /v1/lesson-templates/{id}/active         RequireLessonLibraryWriter   → SetLessonTemplateActive
DELETE /v1/lesson-templates/{id}                RequireLessonLibraryWriter   → SoftDeleteLessonTemplate
POST   /v1/modules/{moduleId}/lessons/from-template  RequireCourseAuthor    → AttachLessonTemplateToModule
```

`RequireCourseAuthor` is the policy added in the create-course plan (`CourseDirector + SystemAdmin + Management`). If it doesn't yet exist, add it in the same PR.

All endpoints follow the established patterns (cursor pagination, Problem Details for errors, `ProducesValidationProblem`, `Results.Created` with Location header on POST).

Register `MapLessonTemplateEndpoints()` in `src/FSBS.Api/Program.cs` next to other endpoint groups.

## UI layer

### New typed HttpClient service

`src/FSBS.Web/Services/LessonTemplateService.cs` — mirrors `InvitationService`:

```csharp
public sealed class LessonTemplateService(HttpClient http)
{
    public Task<CursorPage<LessonTemplateListItemDto>> ListAsync(LessonTemplateFilter filter, string? cursor, int limit, CancellationToken ct);
    public Task<LessonTemplateDto> GetAsync(Guid id, CancellationToken ct);
    public Task<LessonTemplateDto> CreateAsync(CreateLessonTemplateRequest request, CancellationToken ct);
    public Task<LessonTemplateDto> UpdateAsync(Guid id, UpdateLessonTemplateRequest request, CancellationToken ct);
    public Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct);
    public Task DeleteAsync(Guid id, CancellationToken ct);
    public Task<LessonDto> AttachToModuleAsync(Guid moduleId, AttachLessonToModuleRequest request, CancellationToken ct);
}
```

Register `builder.Services.AddScoped<LessonTemplateService>();` in `FSBS.Web/Program.cs`.

### New pages (under `src/FSBS.Web/Pages/Staff/LessonLibrary/`)

All pages use the existing pattern: `EditForm` + `DataAnnotationsValidator` + MudBlazor controls.

| Route | Page | Auth | Purpose |
|---|---|---|---|
| `/staff/lesson-library` | `LessonLibraryList.razor` | `Roles="SystemAdmin,CourseDirector,Management,SalesStaff,Instructor"` | Paginated grid with filters (training type, category, active). Each row → Edit (writers only) / View (readers). |
| `/staff/lesson-library/new` | `CreateLessonTemplate.razor` | `Roles="SystemAdmin,CourseDirector"` | Form to create a template. |
| `/staff/lesson-library/{id:guid}` | `LessonTemplateDetail.razor` | reader roles | Read-only detail + "Used in N lessons". Writers see Edit/Retire/Delete buttons. |
| `/staff/lesson-library/{id:guid}/edit` | `EditLessonTemplate.razor` | writer roles | Pre-filled form. Concurrency: catch 409 and surface "Template was modified — reload?" |

UX guard rails:
- "Active" toggle and "Delete" both show a `MudDialog` confirmation if `usageCount > 0` ("This template is attached to N module lessons. Existing attachments are unaffected. Continue?").
- TrainingType filter defaults to "All" but persists across navigations via `Blazored.LocalStorage` (matches existing user-pref pattern).

### Integration into the existing Course create / Module edit flows

- In `CreateCourse.razor` (from the prior plan) and the future `EditModule.razor`, the "Add lesson" action gets a new option **"From library"** → opens a picker that calls `LessonTemplateService.ListAsync` with `trainingType = course.TrainingType` pre-filtered. Selecting a template + sequence number → calls `AttachToModuleAsync` → returns the freshly-inserted `Lesson`.
- Keep the existing "Add custom lesson" path as a fallback for one-offs that don't warrant a library entry.

### Nav

Add a sidebar link **"Lesson library"** under a new "Curriculum" group, visible to all reader roles. Mirror how Staff/Invitations is exposed in `src/FSBS.Web/Layout/`.

## Files summary

**Create**
- `src/FSBS.Domain/Entities/LessonTemplate.cs`
- `src/FSBS.Domain/Interfaces/ILessonTemplateRepository.cs`
- `src/FSBS.Domain/Interfaces/ILessonRepository.cs`
- `src/FSBS.Infrastructure.Persistence.Entities/Configurations/LessonTemplateConfiguration.cs`
- `src/FSBS.Infrastructure.Persistence.Repositories/LessonTemplateRepository.cs`
- `src/FSBS.Infrastructure.Persistence.Repositories/LessonTemplateReadRepository.cs`
- `src/FSBS.Infrastructure.Persistence.Repositories/LessonRepository.cs`
- `src/FSBS.Infrastructure.Persistence.Repositories.Interfaces/ILessonTemplateReadRepository.cs`
- `src/FSBS.Infrastructure.Persistence.Migrations/Migrations/{timestamp}_AddLessonLibrary.cs`
- `src/FSBS.Application/LessonLibrary/Commands/{CreateLessonTemplate,UpdateLessonTemplate,SetLessonTemplateActive,SoftDeleteLessonTemplate,AttachLessonTemplateToModule}{Command,Handler,CommandValidator}.cs`
- `src/FSBS.Application/LessonLibrary/Queries/{ListLessonTemplates,GetLessonTemplateById}{Query,Handler}.cs`
- `src/FSBS.Shared/LessonLibrary/{LessonTemplateDto,LessonTemplateListItemDto,CreateLessonTemplateRequest,UpdateLessonTemplateRequest,SetActiveRequest,AttachLessonToModuleRequest,AttachLessonResponse}.cs`
- `src/FSBS.Api/Endpoints/LessonTemplateEndpoints.cs`
- `src/FSBS.Web/Services/LessonTemplateService.cs`
- `src/FSBS.Web/Pages/Staff/LessonLibrary/{LessonLibraryList,CreateLessonTemplate,LessonTemplateDetail,EditLessonTemplate}.razor`
- `src/FSBS.Web/Pages/Staff/LessonLibrary/Components/LessonTemplatePicker.razor` (reused dialog for Module/Course flows)
- `tests/FSBS.Application.Tests/LessonLibrary/*Tests.cs`
- `tests/FSBS.Integration.Tests/LessonLibrary/*EndpointTests.cs`

**Modify**
- `src/FSBS.Domain/Entities/Lesson.cs` — add `SourceTemplateId` + nav.
- `src/FSBS.Infrastructure.Persistence.Entities/Configurations/LessonConfiguration.cs` — wire optional FK to `LessonTemplate`.
- `src/FSBS.Infrastructure.Persistence/FsbsDbContext.cs` — add `DbSet<LessonTemplate> LessonTemplates`.
- `src/FSBS.Infrastructure.Persistence.Repositories/RepositoriesServiceExtensions.cs` — register both new repos.
- `src/FSBS.Api/Program.cs` — add `RequireLessonLibraryWriter` + `RequireLessonLibraryReader` policies; wire `MapLessonTemplateEndpoints`.
- `src/FSBS.Web/Program.cs` — register `LessonTemplateService`.
- `src/FSBS.Web/Layout/` nav file — add "Curriculum → Lesson library" link.
- `src/FSBS.Web/Pages/Staff/Courses/CreateCourse.razor` (from prior plan) and any future `EditModule.razor` — add "Add from library" action.

## Verification

1. **Domain / unit tests**
   - `LessonTemplate` factory: required fields enforced.
   - `CreateLessonTemplateHandler` — sets `TenantId` from `ICurrentUser`; persists via repo.
   - `AttachLessonTemplateToModuleHandler` — copies fields; sets `SourceTemplateId`; rejects when `template.TrainingType != course.TrainingType`; rejects when template is inactive or soft-deleted.
   - Validators — boundary cases for duration / lengths / training type enum.

2. **Integration tests** (Testcontainers Postgres)
   - `POST /v1/lesson-templates` with `CourseDirector` JWT → `201`; row exists; tenant_id matches JWT.
   - `POST` with `SalesStaff` → `403`.
   - `GET /v1/lesson-templates` with `Instructor` → `200`.
   - `GET` with `PrivateCustomer` → `403`.
   - `POST /v1/modules/{id}/lessons/from-template` happy path → new `lessons` row exists, `source_template_id` set.
   - `POST .../from-template` with mismatched `TrainingType` → `400` with descriptive validation error.
   - `POST .../from-template` reusing existing `(module_id, sequence_order)` → `409 Conflict`.
   - Soft-delete template, then attach → `400` ("template is not active").

3. **RLS / multi-tenancy**
   - Confirm a tenant cannot read another tenant's templates (set `app.current_tenant_id` to a different tenant and re-query — expect empty).

4. **Manual UI**
   - Log in as `CourseDirector` → navigate to `/staff/lesson-library` → create 3 templates → list shows them.
   - Navigate to course-create page → "Add from library" → picker shows only templates matching the course's `TrainingType`.
   - Log in as `SalesStaff` → library page is read-only (no Create/Edit buttons rendered).
   - Log in as `Instructor` → library page accessible; "Edit" and "Delete" hidden.
   - Soft-delete a template, refresh list → it disappears from the default view; usage-count badge on any modules built from it remains.

5. **Migration safety**
   - Run migration on staging snapshot. Existing `lessons` rows get `source_template_id = NULL` (no backfill required). Confirm no foreign-key violations and that the global query filter on `LessonTemplate` doesn't accidentally filter joined-on queries against `lessons`.