# Plan — CourseDirector creates a Course (with initial Modules)

## Context

`Course` and its child entities (`Module`, `Lesson`, `Enrolment`) exist in `FSBS.Domain` with full EF Core configuration, but there is **no** CQRS handler, API endpoint, repository, DTO, or Blazor page for Courses today (the `CourseService.cs` in `FSBS.Web` is a stub). Per `CLAUDE.md`, **CourseDirector** is the role that owns courses, enrolments, and progress sign-off. This plan adds the first vertical slice: a CourseDirector (or SystemAdmin/Management — per user choice) can create a Course **together with an ordered list of initial Modules** via the Blazor admin UI, with everything persisted in a single transaction.

**Scope confirmed with user:**
- One-shot: Course shell + ordered initial Modules (Lessons are out of scope; added later).
- Blazor form library: `EditForm` + `DataAnnotationsValidator` + MudBlazor controls (matches `IssueCorporateInvitation.razor`).
- Endpoint auth (write): allow `CourseDirector`, `SystemAdmin`. Management is **read-only** for course content, matching the role table in `CLAUDE.md`; they'll get the future GET endpoints but not POST/PUT/DELETE.

## Pattern being mirrored

The `CreateCorporateManagerInvitation` flow is the cleanest small example. We mirror its structure end-to-end:

- Command + Validator + Handler in `FSBS.Application/{Feature}/Commands/`
- Endpoint group in `FSBS.Api/Endpoints/`
- Write repo in `FSBS.Domain/Interfaces/` + impl in `FSBS.Infrastructure.Persistence.Repositories/`
- DTOs in `FSBS.Shared/`
- Typed `HttpClient` service in `FSBS.Web/Services/` + Blazor page in `FSBS.Web/Pages/Staff/Courses/`

Key reusable types found:
- `ICommand<T>` — `src/FSBS.Application/Common/Interfaces/ICommand.cs`
- `ICurrentUser` (gives `UserId`, `TenantId`, `Role`) — `src/FSBS.Application/Common/Interfaces/ICurrentUser.cs`
- MediatR pipeline `LoggingBehaviour` → `ValidationBehaviour` → `TransactionBehaviour` (transaction is supplied for us; handler does NOT call `SaveChangesAsync` directly when `TransactionBehaviour` wraps it — but the Invitation handler does call `SaveChangesAsync` itself; we will mirror exactly what the exemplar does in that repo to stay consistent).
- Existing auth policy `RequireCourseDirector` — `src/FSBS.Api/Program.cs:158`

## Application layer

### New: `src/FSBS.Application/Courses/Commands/CreateCourseCommand.cs`

```csharp
public record CreateCourseModuleInput(
    string Title,
    int SequenceOrder,
    string? Description);

public record CreateCourseCommand(
    string Title,
    string? Description,
    string? RegulatoryFramework,
    decimal TotalHours,
    TrainingType TrainingType,
    bool IsActive,
    IReadOnlyList<CreateCourseModuleInput> Modules)
    : ICommand<CreateCourseResult>;
```

### New: `src/FSBS.Application/Courses/Commands/CreateCourseResult.cs`

```csharp
public record CreateCourseResult(
    Guid CourseId,
    string Title,
    TrainingType TrainingType,
    int ModuleCount);
```

### New: `src/FSBS.Application/Courses/Commands/CreateCourseCommandValidator.cs`

FluentValidation rules:
- `Title`: NotEmpty, MaxLength(300).
- `Description`: MaxLength(4000) when not null.
- `RegulatoryFramework`: MaxLength(100) when not null.
- `TotalHours`: GreaterThan(0), LessThan(10000), `PrecisionScale(6,1,true)`.
- `TrainingType`: `IsInEnum()`.
- `Modules`: NotNull. When non-empty:
  - Each `Title`: NotEmpty, MaxLength(200).
  - Each `Description`: MaxLength(4000) when not null.
  - Each `SequenceOrder`: GreaterThanOrEqualTo(1) (matches `ModuleConfiguration` CHECK).
  - Collection-level: `SequenceOrder` values must be **unique** within the request (mirrors the `(course_id, sequence_order)` unique index in `ModuleConfiguration.cs`).
  - Cap module count at, e.g., 50 to keep payloads sane.

### New: `src/FSBS.Application/Courses/Commands/CreateCourseHandler.cs`

Responsibilities:
1. Inject `ICurrentUser`, `ICourseRepository`, `ILogger<CreateCourseHandler>`.
2. Build a new `Course` entity (set `TenantId = currentUser.TenantId`, `IsActive`, `TrainingType`, scalar fields).
3. Build `Module` children from `command.Modules`, attaching them to `course.Modules`. Don't set `TenantId` on Module (not tenant-scoped per the entity).
4. Call `courseRepository.AddAsync(course, ct)` — repo adds + persists. Audit columns are stamped by the existing audit interceptor.
5. Return `CreateCourseResult(course.Id, course.Title, course.TrainingType, course.Modules.Count)`.
6. Log info: `"Course {CourseId} created by {UserId} in tenant {TenantId} with {ModuleCount} modules"`.

No domain events emitted in this slice — there's no notification yet. If/when a `CourseCreated` event is wanted (e.g. to email assigned instructors), it slots in here later.

## Repository

### New: `src/FSBS.Domain/Interfaces/ICourseRepository.cs`

```csharp
public interface ICourseRepository
{
    Task AddAsync(Course course, CancellationToken ct = default);
    Task<Course?> FindByIdAsync(Guid id, CancellationToken ct = default);
}
```

Read-side projection repos (DTO-returning) will be added in a later read slice — not needed for create.

### New: `src/FSBS.Infrastructure.Persistence.Repositories/CourseRepository.cs`

- `AddAsync` → `db.Courses.Add(course); await db.SaveChangesAsync(ct);` (mirrors `InvitationRepository.CreateAsync`).
- `FindByIdAsync` → `db.Courses.Include(c => c.Modules).FirstOrDefaultAsync(c => c.Id == id, ct)` — used by the GET-by-id endpoint added in the read slice (out of scope, but interface defined now for symmetry).

### Modify: `src/FSBS.Infrastructure.Persistence.Repositories/RepositoriesServiceExtensions.cs`

Add `services.AddScoped<ICourseRepository, CourseRepository>();` to the existing `AddRepositories` block.

## API layer

### New: `src/FSBS.Shared/Courses/` DTOs

- `CreateCourseRequest.cs` — mirrors command shape:
  ```csharp
  public record CreateCourseRequest(
      string Title,
      string? Description,
      string? RegulatoryFramework,
      decimal TotalHours,
      TrainingType TrainingType,
      bool IsActive,
      IReadOnlyList<CreateCourseModuleRequest> Modules);

  public record CreateCourseModuleRequest(string Title, int SequenceOrder, string? Description);
  ```
- `CreateCourseResponse.cs` — matches `CreateCourseResult` (`CourseId`, `Title`, `TrainingType`, `ModuleCount`).
- `CourseDto.cs` (used by future read endpoints — defined now so the front-end has a stable shape) with read-side projection fields.

### New: `src/FSBS.Api/Endpoints/CourseEndpoints.cs`

Mirror `InvitationEndpoints.cs`:

```csharp
public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder app)
{
    var group = app.MapGroup("/v1/courses")
        .WithTags("Courses")
        .RequireAuthorization("RequireCourseAuthor"); // see policy note below

    group.MapPost("/", CreateAsync)
        .WithName("CreateCourse")
        .WithSummary("Create a Course (with optional initial Modules).")
        .Produces<CreateCourseResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict);

    return app;
}

private static async Task<IResult> CreateAsync(
    [FromBody] CreateCourseRequest request,
    ISender sender,
    CancellationToken ct)
{
    var command = new CreateCourseCommand(
        request.Title,
        request.Description,
        request.RegulatoryFramework,
        request.TotalHours,
        request.TrainingType,
        request.IsActive,
        request.Modules.Select(m => new CreateCourseModuleInput(m.Title, m.SequenceOrder, m.Description)).ToList());

    var result = await sender.Send(command, ct);
    var response = new CreateCourseResponse(result.CourseId, result.Title, result.TrainingType, result.ModuleCount);
    return Results.Created($"/v1/courses/{result.CourseId}", response);
}
```

Register in `Program.cs` alongside the other `Map*Endpoints` calls.

### Modify: `src/FSBS.Api/Program.cs`

Add (or repurpose) a policy that accepts the three roles. Cleanest is a new composite policy so we don't widen the existing `RequireCourseDirector`:

```csharp
.AddPolicy("RequireCourseAuthor", p => p.RequireAssertion(ctx =>
    ctx.User.HasClaim("app_role", "CourseDirector") ||
    ctx.User.HasClaim("app_role", "SystemAdmin")))
```

## UI layer

### New: `src/FSBS.Web/Services/CourseService.cs`

Replace the stubbed `CourseService`. Typed `HttpClient` service, registered with `builder.Services.AddScoped<CourseService>();` in `FSBS.Web/Program.cs` (mirroring line 36 for `InvitationService`).

```csharp
public sealed class CourseService(HttpClient http)
{
    public async Task<CreateCourseResponse> CreateAsync(CreateCourseRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("v1/courses", request, ct);
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new InvalidOperationException("A course with conflicting data already exists.");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreateCourseResponse>(ct)
            ?? throw new InvalidOperationException("Unexpected empty response from server.");
    }
}
```

### New: `src/FSBS.Web/Pages/Staff/Courses/CreateCourse.razor`

- Route: `@page "/staff/courses/create"`
- Auth: `@attribute [Authorize(Roles = "CourseDirector,SystemAdmin")]`
- Form library: `EditForm` + `DataAnnotationsValidator` (mirrors `IssueCorporateInvitation.razor`).
- State: local component model (no Fluxor needed — single-form, non-wizard).
- Layout: MudBlazor (`MudCard`, `MudTextField`, `MudNumericField`, `MudSelect<TrainingType>`, `MudSwitch`, `MudButton`).

**View model**

```csharp
public sealed class CreateCourseFormModel
{
    [Required, MaxLength(300)] public string Title { get; set; } = "";
    [MaxLength(4000)]          public string? Description { get; set; }
    [MaxLength(100)]           public string? RegulatoryFramework { get; set; }
    [Range(0.1, 9999.9)]       public decimal TotalHours { get; set; } = 1;
    [Required]                 public TrainingType TrainingType { get; set; } = TrainingType.FlightDeck;
                                public bool IsActive { get; set; } = true;
                                public List<ModuleRow> Modules { get; } = new();
}

public sealed class ModuleRow
{
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [Range(1, 999)]            public int SequenceOrder { get; set; }
    [MaxLength(4000)]          public string? Description { get; set; }
}
```

**UX details**
- Modules section: a repeating `MudTable` (or simple stacked rows) with **Add module** and per-row **Remove** buttons. Auto-suggest the next `SequenceOrder` on Add (1, 2, 3, …) but allow override.
- Client-side guard: warn if duplicate `SequenceOrder` values appear (server validator is the source of truth, but pre-empting the round-trip is nicer).
- Submit: disable while in flight (`_isSubmitting` bool), show `MudProgressCircular`. On success, show `MudAlert(Success)` with `Results.Created` info and offer "Create another" / navigate to the (not-yet-built) Course detail page.
- Validation errors from the server: read `ProblemDetails` and surface via `MudAlert(Error)`.

### Modify: `src/FSBS.Web/Layout/` nav

Add a "Courses" → "Create course" entry visible when `user.IsInRole("CourseDirector" | "SystemAdmin")`. Mirror how Staff/Invitations is exposed in the existing nav layout. (Management will see the courses list once the read slice ships, but not the Create link.)

## Files summary

**Create**
- `src/FSBS.Application/Courses/Commands/CreateCourseCommand.cs`
- `src/FSBS.Application/Courses/Commands/CreateCourseResult.cs`
- `src/FSBS.Application/Courses/Commands/CreateCourseCommandValidator.cs`
- `src/FSBS.Application/Courses/Commands/CreateCourseHandler.cs`
- `src/FSBS.Domain/Interfaces/ICourseRepository.cs`
- `src/FSBS.Infrastructure.Persistence.Repositories/CourseRepository.cs`
- `src/FSBS.Shared/Courses/CreateCourseRequest.cs`
- `src/FSBS.Shared/Courses/CreateCourseModuleRequest.cs`
- `src/FSBS.Shared/Courses/CreateCourseResponse.cs`
- `src/FSBS.Shared/Courses/CourseDto.cs` *(scaffold for read-slice reuse)*
- `src/FSBS.Api/Endpoints/CourseEndpoints.cs`
- `src/FSBS.Web/Pages/Staff/Courses/CreateCourse.razor` (+ `.razor.cs` if you split code-behind)
- `tests/FSBS.Application.Tests/Courses/CreateCourseHandlerTests.cs`
- `tests/FSBS.Integration.Tests/Courses/CreateCourseEndpointTests.cs`

**Modify**
- `src/FSBS.Infrastructure.Persistence.Repositories/RepositoriesServiceExtensions.cs` — register `ICourseRepository`.
- `src/FSBS.Api/Program.cs` — register `RequireCourseAuthor` policy; wire `MapCourseEndpoints`.
- `src/FSBS.Web/Program.cs` — register `CourseService`.
- `src/FSBS.Web/Services/CourseService.cs` — replace stub.
- `src/FSBS.Web/Layout/` nav file — add menu entry.

## Verification

1. **Unit tests (Application)** — `CreateCourseHandlerTests` with NSubstitute on `ICourseRepository` and `ICurrentUser`:
   - Happy path persists course with `TenantId` from `ICurrentUser`.
   - Modules are attached to the course in the right order with correct `SequenceOrder`.
   - Empty `Modules` list is allowed.
2. **Validator tests** — duplicate `SequenceOrder` → invalid; `TotalHours <= 0` → invalid; oversized title → invalid; `TrainingType` outside enum → invalid.
3. **Integration test (Testcontainers PostgreSQL)** — `POST /v1/courses` with a `CourseDirector` JWT returns `201`; row exists in `courses`; child rows exist in `modules`; audit columns are stamped; `tenant_id` matches the JWT.
4. **AuthZ negative** — `POST /v1/courses` with `PrivateCustomer` JWT → `403`; with `Instructor` JWT → `403`; with `Management` JWT → `403`; with anonymous → `401`.
5. **Manual UI**:
   - `dotnet run` API + `dotnet run` Web → log in as a `CourseDirector` test user via the existing dev login flow.
   - Navigate to `/staff/courses/create`, fill in form, add 2 modules with sequences 1 and 2, submit → success alert.
   - Repeat with duplicate sequences → server `400` + validation messages surface in `MudAlert`.
   - Open Postgres (or psql via docker-compose) and confirm `courses` + `modules` rows.
6. **DB invariants** — confirm trying to POST with `TotalHours = 0` is rejected (validator first, then DB `CHECK` as second line of defence).
