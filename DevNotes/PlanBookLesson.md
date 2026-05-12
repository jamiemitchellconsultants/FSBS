# Plan — Lesson selection during the booking flow

## Context

Today a student (Internal, Private, Corporate) books simulator time without tying the booking to a specific Lesson. `Booking.EnrolmentId` and `BookingSlot.LessonId` are already nullable columns on the schema — the wiring exists in the data model but no command, endpoint, validator, or UI uses them. `ProgressRecord` links Lesson ↔ Enrolment for completion sign-off, but there's no automatic creation of a planned progress entry when a lesson is booked.

This plan turns those latent columns on: a student picks a Course (from their active Enrolments) and a Lesson within it as an **optional** part of the booking wizard. When supplied, the booking is linked to the Enrolment and the BookingSlot is stamped with the Lesson. After the session is `Completed`, the Instructor / CourseDirector creates a `ProgressRecord` against that Lesson (existing manual flow — no change here).

**Scope confirmed (defaults — flag if you want different):**

| Decision | Default |
|---|---|
| Lesson selection is mandatory? | **Optional.** Preserves existing "open booking" use case for non-enrolled students and ad-hoc training. |
| Lessons per booking | **One** (matches existing `BookingSlot.LessonId` singularity and one-slot-per-booking pattern). Multi-lesson bookings are out of scope. |
| Filter by current student's enrolments | **Yes** — only show lessons within courses the student has an `Active` Enrolment for. |
| Filter by booking TrainingType | **Yes** — only show lessons from courses whose `TrainingType` matches the selected booking `TrainingType`. |
| Hide already-completed lessons | **Yes by default**, with a "show completed (retake)" toggle. |
| Pricing impact | **None** — lesson selection does not change price. Documented explicitly so a future PricingRule can opt in. |
| Approval impact for InternalStudent | **None** — still goes through `PendingApproval`. Lesson + Enrolment are part of the approval payload shown to SalesStaff. |
| InstructorId implied by Lesson | If `Lesson.RequiresInstructor == true`, the wizard requires an InstructorId on submit (validator). |
| Corporate / Private students not enrolled | Lesson step is skipped (no enrolments → "Continue without lesson" CTA). |

## Data model

**No schema migration required.** All needed columns already exist:

- `Booking.EnrolmentId` (nullable `uuid` → `enrolments`)
- `BookingSlot.LessonId` (nullable `uuid` → `lessons`)

### Recommended additions to existing EF configurations (no DB change)

- `BookingConfiguration`: add `HasOne(b => b.Enrolment).WithMany().HasForeignKey(b => b.EnrolmentId).OnDelete(DeleteBehavior.Restrict)` if not already wired (verify during impl).
- `BookingSlotConfiguration`: same for `Lesson`.

### One new index (optional — for the "what's been booked against this lesson" lookup)

```
CREATE INDEX IF NOT EXISTS ix_booking_slots_lesson_id
  ON fsbs.booking_slots (lesson_id)
  WHERE lesson_id IS NOT NULL;
```

Add via a small migration `AddBookingSlotLessonIdIndex`.

## Application layer (CQRS)

### Modify: `BookSimulatorSlotCommand`

`src/FSBS.Application/Bookings/Commands/BookSimulatorSlotCommand.cs`

Add two optional fields:

```csharp
public record BookSimulatorSlotCommand(
    Guid BayId,
    Guid ConfigurationId,
    TrainingType TrainingType,
    DateTimeOffset SlotStart,
    DateTimeOffset SlotEnd,
    int StudentCount,
    Guid IdempotencyKey,
    Guid? InstructorId,
    string? DepartmentName,
    string? BudgetCode,
    // NEW:
    Guid? EnrolmentId,
    Guid? LessonId
) : IRequest<BookSimulatorSlotResult>;
```

### Modify: `BookSimulatorSlotCommandValidator`

New rule cluster, all guarded by `When(x => x.LessonId.HasValue, ...)`:

- `EnrolmentId` must be present whenever `LessonId` is present (and vice versa is OK — lesson without enrolment is invalid; enrolment-only is allowed if you want to attribute a booking to a course without picking a specific lesson).
- (No DB-level cross-checks here — those happen in the handler with the actual records loaded.)

### Modify: `BookSimulatorSlotHandler`

Insert a new validation block after the existing config/slot/instructor checks, before constructing the `Booking`:

1. **Load the Enrolment** via new `IEnrolmentRepository.FindByIdAsync(EnrolmentId)`.
   - 404 / Problem if not found.
   - Reject if `enrolment.UserId != currentUser.UserId` (students can only book against their own enrolments).
   - Reject if `enrolment.Status != EnrolmentStatus.Active`.
2. **Load the Lesson** via new `ILessonRepository.FindByIdWithCourseAsync(LessonId)` (eager-loads `Module.Course`).
   - 404 if not found.
   - Reject if `lesson.Module.CourseId != enrolment.CourseId` (lesson must belong to the enrolled course).
   - Reject if `lesson.Module.Course.TrainingType != command.TrainingType` (training-type capability gate).
   - Reject if `lesson.RequiresInstructor == true && command.InstructorId == null` (matches existing instructor-rating rule — extend the error message to include the lesson title).
3. **Set `Booking.EnrolmentId = command.EnrolmentId`** and **`BookingSlot.LessonId = command.LessonId`** when present.
4. The rest of the handler is unchanged. InternalStudent still goes to `PendingApproval`; external still goes to `Provisional`.

All cross-record lookups feed the validation messages — Problem Details with field paths so the wizard can surface them inline.

### New: read query for the wizard

`src/FSBS.Application/Enrolments/Queries/ListEnrolledLessonsForStudentQuery.cs`

```csharp
public record ListEnrolledLessonsForStudentQuery(
    TrainingType? TrainingType,
    bool IncludeCompleted
) : IRequest<IReadOnlyList<EnrolledCourseLessonsDto>>;
```

Handler responsibilities:
- Resolve `currentUser.UserId`.
- Load all `Active` enrolments for the user via new `IEnrolmentReadRepository.ListActiveByUserAsync(userId, trainingType, ct)`.
- For each enrolment, load the course's modules → lessons → existing `ProgressRecord` entries (for that enrolment) to mark each lesson `IsCompleted`.
- Filter out completed lessons when `!IncludeCompleted`.
- Return DTOs grouped by course (course id/title/training type → ordered list of lessons with completion flag, `MinDurationMins`, `RequiresInstructor`, `IsMandatory`).
- Use Dapper for the projection to avoid EF tracking + N+1 (mirrors other read-projection handlers).

DTOs in `src/FSBS.Shared/Bookings/`:

```csharp
public record EnrolledCourseLessonsDto(
    Guid EnrolmentId,
    Guid CourseId,
    string CourseTitle,
    TrainingType TrainingType,
    IReadOnlyList<EnrolledLessonDto> Lessons);

public record EnrolledLessonDto(
    Guid LessonId,
    Guid ModuleId,
    string ModuleTitle,
    int ModuleSequence,
    int LessonSequence,
    string LessonTitle,
    int MinDurationMins,
    bool RequiresInstructor,
    bool IsMandatory,
    bool IsCompleted);
```

## Repository layer

### New: `IEnrolmentRepository` (write-side)

`src/FSBS.Domain/Interfaces/IEnrolmentRepository.cs`

```csharp
public interface IEnrolmentRepository
{
    Task<Enrolment?> FindByIdAsync(Guid id, CancellationToken ct = default);
}
```

(Add only what the booking handler needs now. Other enrolment commands — e.g. enrol student — will extend this when their slice ships.)

### New: `IEnrolmentReadRepository`

`src/FSBS.Infrastructure.Persistence.Repositories.Interfaces/IEnrolmentReadRepository.cs`

```csharp
public interface IEnrolmentReadRepository
{
    Task<IReadOnlyList<EnrolledCourseLessonsDto>> ListActiveByUserAsync(
        Guid userId, TrainingType? trainingType, bool includeCompleted, CancellationToken ct = default);
}
```

Implementation in `src/FSBS.Infrastructure.Persistence.Repositories/EnrolmentReadRepository.cs` using Dapper. Single query joining `enrolments → courses → modules → lessons` left-joined to `progress_records` (for completion flag). Order by `module.sequence_order, lesson.sequence_order`.

### Extend: `ILessonRepository`

The Lesson Library plan adds `ILessonRepository.AddAsync`. Extend with:

```csharp
Task<Lesson?> FindByIdWithCourseAsync(Guid id, CancellationToken ct = default);
```

Implementation: `db.Lessons.Include(l => l.Module).ThenInclude(m => m.Course).FirstOrDefaultAsync(l => l.Id == id, ct)`.

### Register

Add the three new bindings (`IEnrolmentRepository`, `IEnrolmentReadRepository`, extended `ILessonRepository`) in `RepositoriesServiceExtensions.AddRepositories`.

## API layer

### Modify: request body for `POST /v1/bookings`

`src/FSBS.Shared/Bookings/BookSimulatorSlotRequest.cs` — add `EnrolmentId?` + `LessonId?` (both `Guid?`). Wire through to the command in `BookingEndpoints.cs`. Response DTO unchanged.

`ProducesProblem(StatusCodes.Status400BadRequest)` already covered; new validation failures surface there.

### New: `GET /v1/bookings/lesson-options`

Reader endpoint for the wizard's lesson step.

```
GET /v1/bookings/lesson-options?trainingType={ft|cc}&includeCompleted={bool}
  Auth: RequireAuthorization()      -- any authenticated student role
  Returns: EnrolledCourseLessonsDto[]
```

Registered in `BookingEndpoints.cs`. Authorisation policy `RequireStudent` (compose of `InternalStudent`, `PrivateCustomer`, `CorporateStudent`) — add to `Program.cs` if missing. Staff roles can be allowed too for impersonation/admin views (deferred until needed).

## UI layer

### Wizard step changes

`src/FSBS.Web/Pages/Bookings/BookingWizard.razor`

Insert a **new step "Lesson"** after the `Training Type / StudentCount` step (i.e. step 3 for external customers; step 3 for internal — bumping DepartmentName/BudgetCode to step 4):

| Flow | Steps |
|---|---|
| External (Private, Corporate) | Sim/Bay → TrainingType → **Lesson (optional)** → Pricing → Confirm |
| InternalStudent | Sim/Bay → TrainingType → **Lesson (optional)** → Dept/Budget → Pricing → Confirm |

So both flows gain one step (5 / 6 total in the recommended sequence).

**Step content** — `src/FSBS.Web/Pages/Bookings/Steps/LessonSelectionStep.razor`

1. On step entry, dispatch `LoadLessonOptionsAction(trainingType, includeCompleted: false)` → Fluxor effect calls `BookingClient.ListLessonOptionsAsync` → reducer fills `LessonOptions`.
2. If `LessonOptions` is empty:
   - Show a friendly message: "You're not enrolled in any active course for this training type. You can continue without selecting a lesson."
   - Big secondary CTA: **Continue without lesson** → advances to next step with `SelectedEnrolmentId = null, SelectedLessonId = null`.
3. If non-empty:
   - Group by `CourseTitle` using `MudExpansionPanels`.
   - Within each course, list `Module` → ordered `Lesson` rows (`MudList`).
   - Each row shows `LessonTitle`, badges for `IsMandatory`, `RequiresInstructor`, `MinDurationMins`, and a checkmark if `IsCompleted`.
   - Single-select (radio per row). Selecting a row stores `(EnrolmentId, LessonId)` in Fluxor.
   - Toggle: **Include already-completed lessons** → re-dispatches the action with `includeCompleted: true`.
   - Subtle hint at top: "Optional — pick a lesson to track this session against your course."
4. Validation before Next:
   - If `SelectedLessonId` is set, ensure `MinDurationMins ≤ chosen slot duration` (warn, don't block — the 240-min minimum is the hard rule).
   - If `lesson.RequiresInstructor && !state.InstructorId`, block with "This lesson requires an instructor — go back and select one."

### Fluxor state

`src/FSBS.Web/State/BookingWizard/BookingWizardState.cs` — add:

```csharp
public Guid? SelectedEnrolmentId { get; init; }
public Guid? SelectedLessonId { get; init; }
public IReadOnlyList<EnrolledCourseLessonsDto> LessonOptions { get; init; } = [];
public bool LessonOptionsLoading { get; init; }
public bool LessonOptionsIncludeCompleted { get; init; }
```

Actions/Reducers in `BookingWizard/Actions/` and `Reducers/`:

- `LoadLessonOptionsAction(TrainingType, bool includeCompleted)` → triggers effect.
- `LessonOptionsLoadedAction(IReadOnlyList<...>)` → fills state.
- `SelectLessonAction(Guid enrolmentId, Guid lessonId)` → mutates selection.
- `ClearLessonSelectionAction()` → unset both (for "Continue without lesson").

Effect: `BookingWizardEffects.HandleLoadLessonOptions` → calls `BookingClient.ListLessonOptionsAsync` → dispatches `LessonOptionsLoadedAction`.

### Typed HttpClient

`src/FSBS.Web/Services/BookingClient.cs` — add:

```csharp
public Task<IReadOnlyList<EnrolledCourseLessonsDto>> ListLessonOptionsAsync(
    TrainingType? trainingType, bool includeCompleted, CancellationToken ct = default);
```

Implementation: `GET v1/bookings/lesson-options?trainingType=…&includeCompleted=…`, standard Polly + `EnsureSuccessStatusCode` pattern.

### Submit step

`BookingWizardEffects.HandleSubmitBooking` — include `SelectedEnrolmentId` + `SelectedLessonId` in the `BookSimulatorSlotRequest` body. No other change to confirm/pricing steps.

### Booking detail view (post-create)

Where `BookingDetail.razor` renders booking metadata, show:
- "Course" — links to `/courses/{courseId}` (deferred — view-page not built yet; render plain text for now)
- "Lesson" — `LessonTitle` + `ModuleTitle`

Add to the response DTO so we don't need a second fetch — extend `BookingDto` in `FSBS.Shared/Bookings/` with optional `CourseTitle?`, `LessonTitle?`, `ModuleTitle?`. Populate in the existing `GetBookingByIdQuery` handler (extend the projection).

## Files summary

**Modify (Application / Domain)**
- `src/FSBS.Application/Bookings/Commands/BookSimulatorSlotCommand.cs`
- `src/FSBS.Application/Bookings/Commands/BookSimulatorSlotCommandValidator.cs`
- `src/FSBS.Application/Bookings/Commands/BookSimulatorSlotHandler.cs`
- `src/FSBS.Application/Bookings/Queries/GetBookingByIdQuery.cs` (extend projection)
- `src/FSBS.Domain/Interfaces/ILessonRepository.cs` (extend with `FindByIdWithCourseAsync`)
- `src/FSBS.Infrastructure.Persistence.Repositories/LessonRepository.cs` (impl)
- `src/FSBS.Infrastructure.Persistence.Entities/Configurations/BookingConfiguration.cs` (wire `Enrolment` nav if missing)
- `src/FSBS.Infrastructure.Persistence.Entities/Configurations/BookingSlotConfiguration.cs` (wire `Lesson` nav if missing)

**Modify (API)**
- `src/FSBS.Shared/Bookings/BookSimulatorSlotRequest.cs`
- `src/FSBS.Shared/Bookings/BookingDto.cs`
- `src/FSBS.Api/Endpoints/BookingEndpoints.cs` (extend POST mapping; add lesson-options GET)
- `src/FSBS.Api/Program.cs` (add `RequireStudent` policy if missing)

**Modify (Web)**
- `src/FSBS.Web/Pages/Bookings/BookingWizard.razor` (insert lesson step)
- `src/FSBS.Web/State/BookingWizard/BookingWizardState.cs`
- `src/FSBS.Web/State/BookingWizard/Actions/*` (add lesson actions)
- `src/FSBS.Web/State/BookingWizard/Reducers/*` (handle new actions)
- `src/FSBS.Web/State/BookingWizard/Effects/BookingWizardEffects.cs` (lesson-options effect + submit-payload extension)
- `src/FSBS.Web/Services/BookingClient.cs`
- `src/FSBS.Web/Pages/Bookings/BookingDetail.razor` (show course / lesson)

**Create**
- `src/FSBS.Application/Enrolments/Queries/ListEnrolledLessonsForStudentQuery.cs`
- `src/FSBS.Application/Enrolments/Queries/ListEnrolledLessonsForStudentHandler.cs`
- `src/FSBS.Domain/Interfaces/IEnrolmentRepository.cs`
- `src/FSBS.Infrastructure.Persistence.Repositories/EnrolmentRepository.cs`
- `src/FSBS.Infrastructure.Persistence.Repositories.Interfaces/IEnrolmentReadRepository.cs`
- `src/FSBS.Infrastructure.Persistence.Repositories/EnrolmentReadRepository.cs`
- `src/FSBS.Shared/Bookings/EnrolledCourseLessonsDto.cs`
- `src/FSBS.Shared/Bookings/EnrolledLessonDto.cs`
- `src/FSBS.Web/Pages/Bookings/Steps/LessonSelectionStep.razor`
- `src/FSBS.Infrastructure.Persistence.Migrations/Migrations/{ts}_AddBookingSlotLessonIdIndex.cs`
- `tests/FSBS.Application.Tests/Bookings/BookLessonScenarios.cs`
- `tests/FSBS.Integration.Tests/Bookings/LessonOptionsEndpointTests.cs`

## Verification

1. **Unit tests** (`BookSimulatorSlotHandler`)
   - Booking without lesson — unchanged behaviour.
   - Booking with valid `(EnrolmentId, LessonId)` — `Booking.EnrolmentId` and `BookingSlot.LessonId` are populated; status flow unchanged.
   - Cross-tenant / cross-user enrolment → rejected.
   - Lesson belongs to a different course than the enrolment → rejected.
   - Lesson's course `TrainingType` ≠ command `TrainingType` → rejected.
   - Lesson `RequiresInstructor` but no `InstructorId` → rejected.

2. **Integration tests**
   - `GET /v1/bookings/lesson-options` for a student with two active enrolments returns both courses' lessons grouped correctly.
   - Same endpoint with `includeCompleted=false` hides lessons already in `progress_records`.
   - Endpoint returns empty list for a `PrivateCustomer` with no enrolments — 200 with `[]`.
   - `POST /v1/bookings` with a lesson belonging to a different student's enrolment → 400 + descriptive Problem Details.

3. **Manual UI**
   - Internal student with an Active enrolment on a FlightDeck course → wizard step shows that course's lessons; selecting one → submit → backend persists `enrolment_id` + `lesson_id` (verify via psql).
   - Same student switches `TrainingType` mid-wizard back to Cabin Crew (course only covers FlightDeck) → lesson step shows empty list + "Continue without lesson".
   - Private customer not enrolled in anything → step renders empty-state CTA; submit completes booking with both nullable columns null.
   - InternalStudent flow: lesson + dept + budget all supplied → booking enters `PendingApproval`. SalesStaff dashboard shows lesson on the approval card (extend `BookingDto` projection — already in the modify list).

4. **Regression**
   - Existing booking integration tests pass unchanged (lesson fields default to null in fixtures).
   - SignalR `AvailabilityHub` delta payload unchanged.
   - Pricing quote unchanged (lesson not in `PricingService` inputs — confirmed in survey).
