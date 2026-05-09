using FSBS.Shared.Bookings;

namespace FSBS.Web.State.PendingApprovals;

public record LoadPendingApprovalsAction;
public record PendingApprovalsLoadedAction(IReadOnlyList<BookingSummaryDto> Items);
public record PendingApprovalsLoadErrorAction(string Message);
public record PendingApprovalApprovedAction(Guid BookingId);
public record PendingApprovalRejectedAction(Guid BookingId);
