namespace FSBS.Application.Common.Exceptions;

/// <summary>
/// Thrown when the user attempting to approve a booking is the same user
/// who created it. Enforced here and reinforced by the DB CHECK constraint
/// on booking_approvals.
/// </summary>
public sealed class SelfApprovalException(Guid bookingId)
    : Exception($"Booking {bookingId} cannot be approved by the user who created it.");
