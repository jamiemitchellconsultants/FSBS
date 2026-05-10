using Fluxor;

namespace FSBS.Web.State.InstructorSchedule;

public static class InstructorScheduleReducers
{
    [ReducerMethod]
    public static InstructorScheduleState OnSetInstructor(InstructorScheduleState state, SetScheduleInstructorAction a) =>
        state with { InstructorId = a.InstructorId, Schedule = null, Error = null };

    [ReducerMethod(typeof(LoadScheduleAction))]
    public static InstructorScheduleState OnLoad(InstructorScheduleState state) =>
        state with { IsLoading = true, Error = null };

    [ReducerMethod]
    public static InstructorScheduleState OnLoaded(InstructorScheduleState state, ScheduleLoadedAction a) =>
        state with { IsLoading = false, Schedule = a.Schedule, Error = null };

    [ReducerMethod]
    public static InstructorScheduleState OnLoadFailed(InstructorScheduleState state, ScheduleLoadFailedAction a) =>
        state with { IsLoading = false, Error = a.Message };

    [ReducerMethod]
    public static InstructorScheduleState OnSetView(InstructorScheduleState state, SetScheduleViewAction a) =>
        state with { View = a.View };

    [ReducerMethod]
    public static InstructorScheduleState OnNavigate(InstructorScheduleState state, NavigateScheduleAction a)
    {
        var anchor = state.View == ScheduleViewMode.Week
            ? state.AnchorDate.AddDays(7 * a.Direction)
            : state.AnchorDate.AddMonths(a.Direction);
        return state with { AnchorDate = anchor };
    }

    [ReducerMethod]
    public static InstructorScheduleState OnJumpToDate(InstructorScheduleState state, JumpToDateAction a) =>
        state with { AnchorDate = a.Date, View = a.View ?? state.View };

    [ReducerMethod(typeof(GoToTodayAction))]
    public static InstructorScheduleState OnGoToToday(InstructorScheduleState state) =>
        state with { AnchorDate = DateOnly.FromDateTime(DateTime.Today) };

    [ReducerMethod]
    public static InstructorScheduleState OnMutationFailed(InstructorScheduleState state, ScheduleMutationFailedAction a) =>
        state with { Error = a.Message };
}
