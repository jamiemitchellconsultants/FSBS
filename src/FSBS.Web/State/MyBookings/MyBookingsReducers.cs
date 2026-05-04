using Fluxor;

namespace FSBS.Web.State.MyBookings;

public static class MyBookingsReducers
{
    [ReducerMethod]
    public static MyBookingsState OnSetViewMode(MyBookingsState state, SetBookingViewModeAction a) =>
        state with { ViewMode = a.Mode, Error = null };

    [ReducerMethod]
    public static MyBookingsState OnSetDate(MyBookingsState state, SetBookingSelectedDateAction a) =>
        state with { SelectedDate = a.Date, RangeItems = [], Error = null };

    [ReducerMethod(typeof(LoadMyBookingsAction))]
    public static MyBookingsState OnLoadList(MyBookingsState state) =>
        state with { ListIsLoading = true, Error = null };

    [ReducerMethod]
    public static MyBookingsState OnListLoaded(MyBookingsState state, MyBookingsLoadedAction a)
    {
        var items = a.Append
            ? [..state.ListItems, ..a.Items]
            : a.Items;

        return state with
        {
            ListIsLoading = false,
            ListItems     = items,
            ListNextCursor = a.NextCursor,
            ListHasMore    = a.NextCursor is not null,
        };
    }

    [ReducerMethod(typeof(LoadMyBookingsForRangeAction))]
    public static MyBookingsState OnLoadRange(MyBookingsState state) =>
        state with { RangeIsLoading = true, Error = null };

    [ReducerMethod]
    public static MyBookingsState OnRangeLoaded(MyBookingsState state, MyBookingsForRangeLoadedAction a) =>
        state with { RangeIsLoading = false, RangeItems = a.Items };

    [ReducerMethod]
    public static MyBookingsState OnError(MyBookingsState state, MyBookingsLoadErrorAction a) =>
        state with { ListIsLoading = false, RangeIsLoading = false, Error = a.Message };
}
