using FSBS.Application.Bookings.Commands;
using FSBS.Application.Tests.Common;
using FSBS.Domain.Tests.Builders;

namespace FSBS.Application.Tests.Bookings;

/// <summary>
/// Smoke test for the handler-fixture scaffold. Verifies an idempotent replay
/// short-circuits to the existing booking. Real handler test classes
/// (full validation paths, conflict detection, event emission) live alongside.
/// </summary>
[Trait("Category", "Unit")]
public class BookSimulatorSlotHandlerSmokeTests : HandlerFixtureBase
{
    [Fact]
    public async Task Handle_WhenIdempotencyKeyMatchesExistingBooking_ReturnsExistingId()
    {
        CurrentUser = FakeCurrentUser.InternalStudent();
        var existing = BookingBuilder.ForInternalStudent().Build();

        BookingRepository
            .FindByIdempotencyKeyAsync(existing.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(existing);

        var sut = new BookSimulatorSlotHandler(
            CurrentUser,
            BookingRepository,
            SimulatorRepository,
            ReconfigurationSlotRepository,
            InstructorRepository);

        var slotStart = DateTimeOffset.UtcNow.AddDays(1);
        var command = new BookSimulatorSlotCommand(
            BayId: Guid.NewGuid(),
            ConfigurationId: Guid.NewGuid(),
            TrainingType: TrainingType.FlightDeck,
            SlotStart: slotStart,
            SlotEnd: slotStart.AddHours(4),
            StudentCount: 2,
            IdempotencyKey: existing.IdempotencyKey);

        var result = await sut.Handle(command, CancellationToken.None);

        result.BookingId.Should().Be(existing.Id);
        await BookingRepository.DidNotReceive().AddAsync(Arg.Any<Booking>(), Arg.Any<CancellationToken>());
    }
}
