using Fluxor;

namespace FSBS.Web.State.Calendar;

public static class CalendarReducers
{
    [ReducerMethod]
    public static CalendarState OnSelectSimulator(CalendarState state, SelectCalendarSimulatorAction a) =>
        state with { SimulatorId = a.SimulatorId, AvailabilityGrid = [], ReconfigWindows = [], MaintenanceWindows = [] };

    [ReducerMethod]
    public static CalendarState OnSetWeek(CalendarState state, SetCalendarWeekAction a) =>
        state with { WeekStart = a.WeekStart };

    [ReducerMethod(typeof(LoadCalendarAction))]
    public static CalendarState OnLoad(CalendarState state) =>
        state with { IsLoading = true };

    [ReducerMethod]
    public static CalendarState OnLoaded(CalendarState state, CalendarLoadedAction a) =>
        state with
        {
            IsLoading = false,
            AvailabilityGrid = a.Grid,
            ReconfigWindows = a.ReconfigWindows,
            MaintenanceWindows = a.MaintenanceWindows
        };
}
