using FluentValidation.TestHelper;

namespace FSBS.Application.Tests.Bookings.Validators;

[Trait("Category", "Unit")]
public class BookingSlotValidatorTests
{
    private readonly BookingSlotValidator _validator = new();
    private static readonly DateTimeOffset Future = DateTimeOffset.UtcNow.AddDays(1);

    private static BookSimulatorSlotCommand Command(
        DateTimeOffset start, DateTimeOffset end) =>
        new(
            BayId: Guid.NewGuid(),
            ConfigurationId: Guid.NewGuid(),
            TrainingType: TrainingType.FlightDeck,
            SlotStart: start,
            SlotEnd: end,
            StudentCount: 2,
            IdempotencyKey: Guid.NewGuid());

    [Fact]
    public void SlotStart_InThePast_FailsValidation()
    {
        var past = DateTimeOffset.UtcNow.AddMinutes(-1);
        var command = Command(past, past.AddHours(4));

        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.SlotStart)
            .WithErrorMessage("Slot start must be in the future.");
    }

    [Fact]
    public void SlotEnd_NotAfterStart_FailsValidation()
    {
        var command = Command(Future, Future);

        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.SlotEnd)
            .WithErrorMessage("Slot end must be after slot start.");
    }

    [Theory]
    [InlineData(239)]   // one minute under
    [InlineData(120)]
    [InlineData(1)]
    public void Duration_BelowMinimum_FailsValidation(int durationMins)
    {
        var command = Command(Future, Future.AddMinutes(durationMins));

        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor("Duration")
            .WithErrorMessage("Booking duration must be at least 240 minutes (4 hours).");
    }

    [Theory]
    [InlineData(240)]   // exactly the minimum is allowed
    [InlineData(241)]
    [InlineData(480)]   // 8 hours
    public void Duration_AtOrAboveMinimum_PassesValidation(int durationMins)
    {
        var command = Command(Future, Future.AddMinutes(durationMins));

        _validator.TestValidate(command)
            .ShouldNotHaveValidationErrorFor("Duration");
    }

    [Fact]
    public void MinDurationConstant_IsTheCanonical240Minutes()
    {
        BookingSlotValidator.MinDurationMins.Should().Be(240);
    }
}
