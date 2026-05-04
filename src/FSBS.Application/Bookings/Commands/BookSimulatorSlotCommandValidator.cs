using FluentValidation;

namespace FSBS.Application.Bookings.Commands;

/// <summary>
/// Validates the structural fields of the command (non-empty IDs).
/// Time/duration rules live in <see cref="BookingSlotValidator"/>;
/// capacity and role rules live in <see cref="BookingCapacityValidator"/>.
/// All three are discovered by assembly scanning and run independently
/// by the ValidationBehaviour pipeline.
/// </summary>
public sealed class BookSimulatorSlotCommandValidator : AbstractValidator<BookSimulatorSlotCommand>
{
    public BookSimulatorSlotCommandValidator()
    {
        RuleFor(x => x.BayId).NotEmpty();
        RuleFor(x => x.ConfigurationId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}
