using FluentValidation;

namespace FSBS.Application.Bookings.Commands;

public sealed class RejectBookingCommandValidator : AbstractValidator<RejectBookingCommand>
{
    public RejectBookingCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(10)
            .WithMessage("Rejection reason must be at least 10 characters.");
    }
}
