using FluentValidation.TestHelper;

namespace FSBS.Application.Tests.Bookings.Validators;

[Trait("Category", "Unit")]
public class RejectBookingCommandValidatorTests
{
    private readonly RejectBookingCommandValidator _validator = new();

    [Fact]
    public void EmptyBookingId_Fails()
    {
        _validator
            .TestValidate(new RejectBookingCommand(Guid.Empty, "Reason long enough"))
            .ShouldHaveValidationErrorFor(x => x.BookingId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("nine char")]   // exactly 9 characters
    public void Reason_TooShortOrEmpty_Fails(string reason)
    {
        _validator
            .TestValidate(new RejectBookingCommand(Guid.NewGuid(), reason))
            .ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Theory]
    [InlineData("ten chars!")]              // exactly 10 — boundary
    [InlineData("Insufficient justification provided by booker")]
    public void Reason_AtOrAbove10Chars_Passes(string reason)
    {
        _validator
            .TestValidate(new RejectBookingCommand(Guid.NewGuid(), reason))
            .ShouldNotHaveValidationErrorFor(x => x.Reason);
    }
}
