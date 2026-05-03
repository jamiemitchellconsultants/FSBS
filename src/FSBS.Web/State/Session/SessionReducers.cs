using Fluxor;

namespace FSBS.Web.State.Session;

public static class SessionReducers
{
    [ReducerMethod]
    public static SessionState OnSetSession(SessionState state, SetSessionAction action) =>
        state with
        {
            UserId = action.UserId,
            TenantId = action.TenantId,
            AppRole = action.AppRole,
            OrgId = action.OrgId,
            IsAuthenticated = true
        };

    [ReducerMethod(typeof(ClearSessionAction))]
    public static SessionState OnClearSession(SessionState _) => new();
}
