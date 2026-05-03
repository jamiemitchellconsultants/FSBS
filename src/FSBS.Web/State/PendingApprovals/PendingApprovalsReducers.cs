using Fluxor;

namespace FSBS.Web.State.PendingApprovals;

public static class PendingApprovalsReducers
{
    [ReducerMethod(typeof(LoadPendingApprovalsAction))]
    public static PendingApprovalsState OnLoad(PendingApprovalsState state) =>
        state with { IsLoading = true };

    [ReducerMethod]
    public static PendingApprovalsState OnLoaded(PendingApprovalsState state, PendingApprovalsLoadedAction a) =>
        state with { IsLoading = false, Items = a.Items, LastCursor = a.NextCursor };
}
