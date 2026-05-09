using Fluxor;

namespace FSBS.Web.State.PendingApprovals;

public static class PendingApprovalsReducers
{
    [ReducerMethod(typeof(LoadPendingApprovalsAction))]
    public static PendingApprovalsState OnLoad(PendingApprovalsState state) =>
        state with { IsLoading = true, Error = null };

    [ReducerMethod]
    public static PendingApprovalsState OnLoaded(PendingApprovalsState state, PendingApprovalsLoadedAction a) =>
        state with { IsLoading = false, Items = a.Items };

    [ReducerMethod]
    public static PendingApprovalsState OnError(PendingApprovalsState state, PendingApprovalsLoadErrorAction a) =>
        state with { IsLoading = false, Error = a.Message };

    [ReducerMethod]
    public static PendingApprovalsState OnApproved(PendingApprovalsState state, PendingApprovalApprovedAction a) =>
        state with { Items = state.Items.Where(x => x.Id != a.BookingId).ToList() };

    [ReducerMethod]
    public static PendingApprovalsState OnRejected(PendingApprovalsState state, PendingApprovalRejectedAction a) =>
        state with { Items = state.Items.Where(x => x.Id != a.BookingId).ToList() };
}
