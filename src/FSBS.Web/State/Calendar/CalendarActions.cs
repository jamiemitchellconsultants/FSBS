namespace FSBS.Web.State.Calendar;

public record SelectCalendarSimulatorAction(Guid SimulatorId);
public record SetCalendarWeekAction(DateOnly WeekStart);
public record LoadCalendarAction;
public record CalendarLoadedAction(
    IReadOnlyList<object> Grid,
    IReadOnlyList<object> ReconfigWindows,
    IReadOnlyList<object> MaintenanceWindows);
public record ApplyCalendarDeltaAction(object Delta);
