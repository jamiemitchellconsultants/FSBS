using FluentValidation;

namespace FSBS.Application.Bookings.Commands;

public sealed class ApproveBookingCommandValidator : AbstractValidator<ApproveBookingCommand>
{
    public ApproveBookingCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
    }
}
