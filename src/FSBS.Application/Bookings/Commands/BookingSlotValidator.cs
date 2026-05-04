using FluentValidation;

namespace FSBS.Application.Bookings.Commands;

/// <summary>
/// Enforces the time and duration rules that apply to every simulator booking slot.
/// Registered as a standalone validator so it runs independently via the
/// ValidationBehaviour pipeline before the handler executes.
/// </summary>
public sealed class BookingSlotValidator : AbstractValidator<BookSimulatorSlotCommand>
{
    public const int MinDurationMins = 240;

    public BookingSlotValidator()
    {
        RuleFor(x => x.SlotStart)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("Slot start must be in the future.");

        RuleFor(x => x.SlotEnd)
            .GreaterThan(x => x.SlotStart)
            .WithMessage("Slot end must be after slot start.");

        RuleFor(x => x)
            .Must(x => (x.SlotEnd - x.SlotStart).TotalMinutes >= MinDurationMins)
            .WithName("Duration")
            .WithMessage($"Booking duration must be at least {MinDurationMins} minutes (4 hours).");
    }
}
