using FSBS.Domain.Enums;

namespace FSBS.Application.Bookings.Commands;

/// <param name="BookingId">The newly created (or previously created idempotent) booking.</param>
/// <param name="Status">
/// <c>Provisional</c> for external customers — the slot is held for 15 minutes
/// pending explicit confirmation. <c>PendingApproval</c> for InternalStudents —
/// slot is held indefinitely until SalesStaff acts.
/// </param>
public record BookSimulatorSlotResult(Guid BookingId, BookingStatus Status);
