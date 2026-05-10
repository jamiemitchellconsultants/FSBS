using FSBS.Shared.InstructorSchedule;

namespace FSBS.Web.State.InstructorSchedule;

public record SetScheduleInstructorAction(Guid? InstructorId);
public record LoadScheduleAction;
public record ScheduleLoadedAction(InstructorScheduleDto Schedule);
public record ScheduleLoadFailedAction(string Message);
public record SetScheduleViewAction(ScheduleViewMode View);
public record NavigateScheduleAction(int Direction);
public record JumpToDateAction(DateOnly Date, ScheduleViewMode? View = null);
public record GoToTodayAction;
public record UpsertWeeklyPatternAction(IReadOnlyList<WeeklyPatternSlotDto> Slots);
public record SetSingleDayAction(DateOnly Date, IReadOnlyList<TimeRangeDto> Available);
public record CreateOverrideAction(DateTimeOffset StartAt, DateTimeOffset EndAt, string Type, string? Notes = null);
public record DeleteOverrideAction(Guid OverrideId);
public record ScheduleMutationFailedAction(string Message);
