using FSBS.Application.Tests.Common;
using FSBS.Domain.Entities;
using FSBS.Domain.Events;
using FSBS.Domain.Exceptions;
using FSBS.Domain.Tests.Builders;

namespace FSBS.Application.Tests.Bookings.Handlers;

[Trait("Category", "Unit")]
public class BookSimulatorSlotHandlerTests : HandlerFixtureBase
{
    private static readonly DateTimeOffset Future = DateTimeOffset.UtcNow.AddDays(1);

    private BookSimulatorSlotHandler Build() => new(
        CurrentUser,
        BookingRepository,
        SimulatorRepository,
        ReconfigurationSlotRepository,
        InstructorRepository);

    private static BookSimulatorSlotCommand Command(
        Guid? configId = null,
        Guid? bayId = null,
        Guid? idempotencyKey = null,
        Guid? instructorId = null,
        TrainingType trainingType = TrainingType.FlightDeck,
        int studentCount = 2,
        string? department = null,
        string? budget = null) =>
        new(
            BayId: bayId ?? Guid.NewGuid(),
            ConfigurationId: configId ?? Guid.NewGuid(),
            TrainingType: trainingType,
            SlotStart: Future,
            SlotEnd: Future.AddHours(4),
            StudentCount: studentCount,
            IdempotencyKey: idempotencyKey ?? Guid.NewGuid(),
            InstructorId: instructorId,
            DepartmentName: department,
            BudgetCode: budget);

    private void SetUpValidConfig(Guid configId) =>
        SimulatorRepository
            .FindConfigurationAsync(configId, Arg.Any<CancellationToken>())
            .Returns(new SimulatorConfiguration { Id = configId });

    // ── Idempotency replay ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_IdempotencyReplay_ReturnsExistingBookingAndDoesNotInsert()
    {
        CurrentUser = FakeCurrentUser.InternalStudent();
        var existing = BookingBuilder.ForInternalStudent().Build();

        BookingRepository
            .FindByIdempotencyKeyAsync(existing.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await Build().Handle(
            Command(idempotencyKey: existing.IdempotencyKey), CancellationToken.None);

        result.BookingId.Should().Be(existing.Id);
        result.Status.Should().Be(existing.Status);
        await BookingRepository.DidNotReceive().AddAsync(Arg.Any<Booking>(), Arg.Any<CancellationToken>());
        await SimulatorRepository.DidNotReceive().FindConfigurationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IdempotencyReplay_DoesNotEmitSlotBookedEvent()
    {
        CurrentUser = FakeCurrentUser.InternalStudent();
        var existing = BookingBuilder.ForInternalStudent().Build();
        existing.ClearDomainEvents();   // baseline — no events on a replay

        BookingRepository
            .FindByIdempotencyKeyAsync(existing.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(existing);

        await Build().Handle(
            Command(idempotencyKey: existing.IdempotencyKey), CancellationToken.None);

        existing.DomainEvents.Should().BeEmpty();
    }

    // ── Conflict detection ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_OverlappingBookingSlot_ThrowsBookingConflict()
    {
        CurrentUser = FakeCurrentUser.InternalStudent();
        var configId = Guid.NewGuid();
        var bayId = Guid.NewGuid();
        SetUpValidConfig(configId);

        BookingRepository
            .FindConflictingSlotsAsync(bayId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<BookingSlot> { new() { Id = Guid.NewGuid() } });

        var act = async () => await Build().Handle(
            Command(configId: configId, bayId: bayId), CancellationToken.None);

        await act.Should().ThrowAsync<BookingConflictException>();
        await BookingRepository.DidNotReceive().AddAsync(Arg.Any<Booking>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OverlappingReconfigSlot_ThrowsBookingConflict()
    {
        CurrentUser = FakeCurrentUser.InternalStudent();
        var configId = Guid.NewGuid();
        var bayId = Guid.NewGuid();
        SetUpValidConfig(configId);

        ReconfigurationSlotRepository
            .HasOverlapAsync(bayId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var act = async () => await Build().Handle(
            Command(configId: configId, bayId: bayId), CancellationToken.None);

        await act.Should().ThrowAsync<BookingConflictException>();
    }

    // ── Instructor rating intersection ────────────────────────────────────────

    [Fact]
    public async Task Handle_InstructorWithoutMatchingRating_ThrowsRatingMismatch()
    {
        CurrentUser = FakeCurrentUser.InternalStudent();
        var configId = Guid.NewGuid();
        var instructorId = Guid.NewGuid();
        SetUpValidConfig(configId);

        InstructorRepository
            .FindByUserIdAsync(instructorId, Arg.Any<CancellationToken>())
            .Returns(new Instructor
            {
                Id = Guid.NewGuid(),
                UserId = instructorId,
                TrainingTypeRatings = [TrainingType.CabinCrew],   // not FlightDeck
            });

        var act = async () => await Build().Handle(
            Command(configId: configId, instructorId: instructorId,
                trainingType: TrainingType.FlightDeck),
            CancellationToken.None);

        await act.Should().ThrowAsync<InstructorRatingMismatchException>();
    }

    [Fact]
    public async Task Handle_InstructorWithMatchingRating_Succeeds()
    {
        CurrentUser = FakeCurrentUser.InternalStudent();
        var configId = Guid.NewGuid();
        var instructorId = Guid.NewGuid();
        SetUpValidConfig(configId);

        InstructorRepository
            .FindByUserIdAsync(instructorId, Arg.Any<CancellationToken>())
            .Returns(new Instructor
            {
                Id = Guid.NewGuid(),
                UserId = instructorId,
                TrainingTypeRatings = [TrainingType.FlightDeck, TrainingType.CabinCrew],
            });

        await Build().Handle(
            Command(configId: configId, instructorId: instructorId,
                trainingType: TrainingType.FlightDeck),
            CancellationToken.None);

        await BookingRepository.Received(1).AddAsync(Arg.Any<Booking>(), Arg.Any<CancellationToken>());
    }

    // ── State machine entry & event emission ──────────────────────────────────

    [Fact]
    public async Task Handle_InternalStudent_CreatesPendingApprovalWithApprovalRecord()
    {
        var bookerId = Guid.NewGuid();
        CurrentUser = new FakeCurrentUser(AppRole.InternalStudent, userId: bookerId);
        var configId = Guid.NewGuid();
        SetUpValidConfig(configId);

        Booking? captured = null;
        await BookingRepository.AddAsync(
            Arg.Do<Booking>(b => captured = b), Arg.Any<CancellationToken>());

        await Build().Handle(
            Command(configId: configId, department: "Flight Ops", budget: "FO-2026-001"),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Status.Should().Be(BookingStatus.PendingApproval);
        captured.BookerRole.Should().Be(AppRole.InternalStudent);
        captured.DepartmentName.Should().Be("Flight Ops");
        captured.BudgetCode.Should().Be("FO-2026-001");
        captured.Approval.Should().NotBeNull();
        captured.Approval!.RequestedBy.Should().Be(bookerId);
        captured.Approval.Decision.Should().Be(ApprovalDecision.Pending);
    }

    [Fact]
    public async Task Handle_ExternalCustomer_CreatesProvisionalWithoutApproval()
    {
        CurrentUser = new FakeCurrentUser(AppRole.PrivateCustomer);
        var configId = Guid.NewGuid();
        SetUpValidConfig(configId);

        Booking? captured = null;
        await BookingRepository.AddAsync(
            Arg.Do<Booking>(b => captured = b), Arg.Any<CancellationToken>());

        await Build().Handle(Command(configId: configId), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Status.Should().Be(BookingStatus.Provisional);
        captured.Approval.Should().BeNull();
        captured.DepartmentName.Should().BeNull();   // dropped for non-internal-student callers
        captured.BudgetCode.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Successful_EmitsSlotBookedEventOnAggregate()
    {
        CurrentUser = FakeCurrentUser.InternalStudent();
        var configId = Guid.NewGuid();
        SetUpValidConfig(configId);

        Booking? captured = null;
        await BookingRepository.AddAsync(
            Arg.Do<Booking>(b => captured = b), Arg.Any<CancellationToken>());

        await Build().Handle(
            Command(configId: configId, department: "Ops", budget: "B-001"),
            CancellationToken.None);

        captured!.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SlotBookedEvent>();

        var evt = (SlotBookedEvent)captured.DomainEvents.Single();
        evt.BookingId.Should().Be(captured.Id);
        evt.ConfigurationId.Should().Be(configId);
    }
}
