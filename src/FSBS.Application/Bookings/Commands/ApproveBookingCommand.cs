using FSBS.Application.Common.Interfaces;

namespace FSBS.Application.Bookings.Commands;

/// <summary>
/// Approves a PendingApproval (InternalStudent) booking. Only SalesStaff and
/// SystemAdmin may send this command; that constraint is enforced by the API
/// authorization policy. The reviewer must not be the same user as the booker.
/// Price is calculated and locked here — the booking transitions to Confirmed.
/// </summary>
public record ApproveBookingCommand(Guid BookingId) : ICommand<ApproveBookingResult>;
