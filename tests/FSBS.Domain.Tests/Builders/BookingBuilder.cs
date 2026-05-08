namespace FSBS.Domain.Tests.Builders;

/// <summary>
/// Constructs <see cref="Booking"/> instances for tests with sensible defaults.
/// Use the <c>For…</c> presets to start from a valid baseline, then override fields
/// with the fluent <c>With…</c> methods.
/// </summary>
public sealed class BookingBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _bookedBy = Guid.NewGuid();
    private Guid? _orgId;
    private AppRole _bookerRole = AppRole.PrivateCustomer;
    private TrainingType _trainingType = TrainingType.FlightDeck;
    private Guid _configurationId = Guid.NewGuid();
    private int _studentCount = 1;
    private BookingStatus _status = BookingStatus.Provisional;
    private string? _departmentName;
    private string? _budgetCode;
    private Guid _idempotencyKey = Guid.NewGuid();
    private readonly List<BookingSlot> _slots = [];
    private BookingApproval? _approval;

    public static BookingBuilder ForExternalCustomer() =>
        new BookingBuilder()
            .WithRole(AppRole.PrivateCustomer)
            .WithStatus(BookingStatus.Provisional);

    public static BookingBuilder ForInternalStudent() =>
        new BookingBuilder()
            .WithRole(AppRole.InternalStudent)
            .WithStatus(BookingStatus.PendingApproval)
            .WithDepartment("Flight Ops")
            .WithBudgetCode("FO-2026-001");

    public static BookingBuilder ForCorporateManager(Guid orgId) =>
        new BookingBuilder()
            .WithRole(AppRole.CorporateManager)
            .WithOrg(orgId)
            .WithStatus(BookingStatus.Provisional);

    public BookingBuilder WithId(Guid id) { _id = id; return this; }
    public BookingBuilder WithBookedBy(Guid userId) { _bookedBy = userId; return this; }
    public BookingBuilder WithOrg(Guid orgId) { _orgId = orgId; return this; }
    public BookingBuilder WithRole(AppRole role) { _bookerRole = role; return this; }
    public BookingBuilder WithTrainingType(TrainingType type) { _trainingType = type; return this; }
    public BookingBuilder WithConfiguration(Guid configId) { _configurationId = configId; return this; }
    public BookingBuilder WithStudentCount(int count) { _studentCount = count; return this; }
    public BookingBuilder WithStatus(BookingStatus status) { _status = status; return this; }
    public BookingBuilder WithDepartment(string? name) { _departmentName = name; return this; }
    public BookingBuilder WithBudgetCode(string? code) { _budgetCode = code; return this; }
    public BookingBuilder WithIdempotencyKey(Guid key) { _idempotencyKey = key; return this; }

    public BookingBuilder WithSlot(DateTimeOffset start, DateTimeOffset end, Guid? bayId = null)
    {
        _slots.Add(new BookingSlot
        {
            Id = Guid.NewGuid(),
            BookingId = _id,
            BayId = bayId ?? Guid.NewGuid(),
            StartAt = start,
            EndAt = end,
            DurationMins = (int)(end - start).TotalMinutes,
            SlotStatus = SlotStatus.Scheduled,
        });
        return this;
    }

    public BookingBuilder WithApproval(Guid? requestedBy = null)
    {
        _approval = new BookingApproval
        {
            Id = Guid.NewGuid(),
            BookingId = _id,
            RequestedBy = requestedBy ?? _bookedBy,
            Decision = ApprovalDecision.Pending,
        };
        return this;
    }

    public Booking Build()
    {
        if (_slots.Count == 0)
        {
            var start = DateTimeOffset.UtcNow.AddDays(1);
            WithSlot(start, start.AddHours(4));
        }

        return new Booking
        {
            Id = _id,
            BookedBy = _bookedBy,
            OrgId = _orgId,
            BookerRole = _bookerRole,
            TrainingType = _trainingType,
            ConfigId = _configurationId,
            StudentCount = _studentCount,
            Status = _status,
            DepartmentName = _departmentName,
            BudgetCode = _budgetCode,
            IdempotencyKey = _idempotencyKey,
            Slots = _slots,
            Approval = _approval,
        };
    }
}
