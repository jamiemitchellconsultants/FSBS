using Fluxor;
using FSBS.Shared.InstructorSchedule;
using FSBS.Web.Services;

namespace FSBS.Web.State.InstructorSchedule;

public sealed class InstructorScheduleEffects(
    InstructorScheduleService service,
    IState<InstructorScheduleState> state)
{
    [EffectMethod(typeof(LoadScheduleAction))]
    public async Task LoadAsync(IDispatcher dispatcher) =>
        await ReloadAsync(dispatcher);

    [EffectMethod]
    public async Task OnSetInstructor(SetScheduleInstructorAction _, IDispatcher dispatcher) =>
        await ReloadAsync(dispatcher);

    [EffectMethod]
    public async Task OnSetView(SetScheduleViewAction _, IDispatcher dispatcher) =>
        await ReloadAsync(dispatcher);

    [EffectMethod]
    public async Task OnNavigate(NavigateScheduleAction _, IDispatcher dispatcher) =>
        await ReloadAsync(dispatcher);

    [EffectMethod]
    public async Task OnJump(JumpToDateAction _, IDispatcher dispatcher) =>
        await ReloadAsync(dispatcher);

    [EffectMethod(typeof(GoToTodayAction))]
    public async Task OnToday(IDispatcher dispatcher) =>
        await ReloadAsync(dispatcher);

    [EffectMethod]
    public async Task OnUpsertPattern(UpsertWeeklyPatternAction action, IDispatcher dispatcher)
    {
        try
        {
            var instructorId = state.Value.InstructorId;
            var body = new WeeklyPatternUpsertRequest(action.Slots);
            if (instructorId is { } id)
                await service.UpsertPatternAsync(id, body);
            else
                await service.UpsertMyPatternAsync(body);

            await ReloadAsync(dispatcher);
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new ScheduleMutationFailedAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task OnSetSingleDay(SetSingleDayAction action, IDispatcher dispatcher)
    {
        try
        {
            var instructorId = state.Value.InstructorId;
            var body = new SingleDayUpsertRequest(action.Available);
            if (instructorId is { } id)
                await service.SetSingleDayAsync(id, action.Date, body);
            else
                await service.SetMySingleDayAsync(action.Date, body);

            await ReloadAsync(dispatcher);
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new ScheduleMutationFailedAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task OnCreateOverride(CreateOverrideAction action, IDispatcher dispatcher)
    {
        try
        {
            var instructorId = state.Value.InstructorId;
            var body = new OverrideRequest(action.StartAt, action.EndAt, action.Type, action.Notes);
            if (instructorId is { } id)
                await service.CreateOverrideAsync(id, body);
            else
                await service.CreateMyOverrideAsync(body);

            await ReloadAsync(dispatcher);
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new ScheduleMutationFailedAction(ex.Message));
        }
    }

    // amazonq-ignore-next-line
    [EffectMethod]
    public async Task OnDeleteOverride(DeleteOverrideAction action, IDispatcher dispatcher)
    {
        try
        {
            var instructorId = state.Value.InstructorId;
            if (instructorId is { } id)
                await service.DeleteOverrideAsync(id, action.OverrideId);
            else
                await service.DeleteMyOverrideAsync(action.OverrideId);

            await ReloadAsync(dispatcher);
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new ScheduleMutationFailedAction(ex.Message));
        }
    }

    private async Task ReloadAsync(IDispatcher dispatcher)
    {
        var current = state.Value;
        var (from, to) = ComputeWindow(current.AnchorDate, current.View);

        try
        {
            var schedule = current.InstructorId is { } id
                ? await service.GetScheduleAsync(id, from, to)
                : await service.GetMyScheduleAsync(from, to);
            dispatcher.Dispatch(new ScheduleLoadedAction(schedule));
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new ScheduleLoadFailedAction(ex.Message));
        }
    }

    public static (DateOnly From, DateOnly To) ComputeWindow(DateOnly anchor, ScheduleViewMode view)
    {
        if (view == ScheduleViewMode.Week)
        {
            var dow = (int)anchor.DayOfWeek;
            var offsetToMonday = (dow + 6) % 7;
            var monday = anchor.AddDays(-offsetToMonday);
            return (monday, monday.AddDays(6));
        }

        var firstOfMonth = new DateOnly(anchor.Year, anchor.Month, 1);
        var monthOffset = ((int)firstOfMonth.DayOfWeek + 6) % 7;
        var gridStart = firstOfMonth.AddDays(-monthOffset);
        return (gridStart, gridStart.AddDays(41));
    }
}
