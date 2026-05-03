namespace FSBS.Domain.Enums;

public enum BookingStatus
{
    Provisional,
    PendingApproval,
    Confirmed,
    InProgress,
    Completed,
    Invoiced,
    CancelledByCustomer,
    CancelledByAdmin,
    Rejected,
    Expired,
    OnHold
}
