namespace FSBS.Web.State.PendingApprovals;

public record LoadPendingApprovalsAction;
public record PendingApprovalsLoadedAction(IReadOnlyList<object> Items, string? NextCursor);
public record ApprovePendingBookingAction(Guid BookingId);
public record RejectPendingBookingAction(Guid BookingId, string Reason);
