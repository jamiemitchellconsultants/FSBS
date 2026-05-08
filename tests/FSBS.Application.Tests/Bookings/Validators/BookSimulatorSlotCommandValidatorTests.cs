using FluentValidation.TestHelper;

namespace FSBS.Application.Tests.Bookings.Validators;

[Trait("Category", "Unit")]
public class BookSimulatorSlotCommandValidatorTests
{
    private readonly BookSimulatorSlotCommandValidator _validator = new();
    private static readonly DateTimeOffset Future = DateTimeOffset.UtcNow.AddDays(1);

    private static BookSimulatorSlotCommand Command(
        Guid? bayId = null,
        Guid? configId = null,
        Guid? idempotencyKey = null) =>
        new(
            BayId: bayId ?? Guid.NewGuid(),
            ConfigurationId: configId ?? Guid.NewGuid(),
            TrainingType: TrainingType.FlightDeck,
            SlotStart: Future,
            SlotEnd: Future.AddHours(4),
            StudentCount: 2,
            IdempotencyKey: idempotencyKey ?? Guid.NewGuid());

    [Fact]
    public void EmptyBayId_Fails()
    {
        _validator.TestValidate(Command(bayId: Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.BayId);
    }

    [Fact]
    public void EmptyConfigurationId_Fails()
    {
        _validator.TestValidate(Command(configId: Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.ConfigurationId);
    }

    [Fact]
    public void EmptyIdempotencyKey_Fails()
    {
        _validator.TestValidate(Command(idempotencyKey: Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    [Fact]
    public void AllStructuralFieldsPresent_Passes()
    {
        _validator.TestValidate(Command()).IsValid.Should().BeTrue();
    }
}
