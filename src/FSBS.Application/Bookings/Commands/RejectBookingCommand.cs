using FSBS.Application.Common.Interfaces;

namespace FSBS.Application.Bookings.Commands;

/// <summary>
/// Rejects a PendingApproval (InternalStudent) booking. Only SalesStaff and
/// SystemAdmin may send this command; that constraint is enforced by the API
/// authorization policy. The reviewer must not be the same user as the booker.
/// All reserved slots are released and orphaned reconfiguration slots are removed.
/// </summary>
public record RejectBookingCommand(
    Guid BookingId,
    string Reason) : ICommand<RejectBookingResult>;
