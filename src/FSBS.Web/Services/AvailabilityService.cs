using System.Net.Http.Json;

namespace FSBS.Web.Services;

public sealed class AvailabilityService(HttpClient http)
{
    public async Task<WeekReconfigurationResult> GetWeekReconfigurationAsync(
        DateOnly weekStart,
        Guid? simulatorId = null,
        CancellationToken ct = default)
    {
        var normalizedWeekStart = GetWeekStart(weekStart);
        var weekEndExclusive = normalizedWeekStart.AddDays(7);

        IReadOnlyList<Guid> simulatorIds;
        if (simulatorId.HasValue)
        {
            simulatorIds = [simulatorId.Value];
        }
        else
        {
            try
            {
                simulatorIds = await GetSimulatorIdsAsync(ct);
            }
            catch
            {
                return new WeekReconfigurationResult(normalizedWeekStart, [], "Unable to load simulators for availability.");
            }
        }

        if (simulatorIds.Count == 0)
        {
            return new WeekReconfigurationResult(normalizedWeekStart, [], "No simulators are available to query.");
        }

        var from = new DateTimeOffset(normalizedWeekStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var to = new DateTimeOffset(weekEndExclusive.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var windows = new List<WeekReconfigurationWindow>();
        foreach (var currentSimulatorId in simulatorIds)
        {
            AvailabilityResponse? response;
            try
            {
                response = await http.GetFromJsonAsync<AvailabilityResponse>(
                    $"v1/simulators/{currentSimulatorId}/availability?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}",
                    ct);
            }
            catch
            {
                continue;
            }

            if (response?.ReconfigurationWindows is null)
            {
                continue;
            }

            windows.AddRange(response.ReconfigurationWindows.Select(x => new WeekReconfigurationWindow(
                x.StartAt,
                x.EndAt,
                x.DurationMins,
                currentSimulatorId)));
        }

        return new WeekReconfigurationResult(normalizedWeekStart, windows.OrderBy(x => x.StartAt).ToList(), null);
    }

    public async Task<MonthAvailabilityResult> GetMonthAvailabilityAsync(
        DateOnly monthStart,
        Guid? simulatorId = null,
        CancellationToken ct = default)
    {
        var normalizedMonthStart = new DateOnly(monthStart.Year, monthStart.Month, 1);
        var monthEndExclusive = normalizedMonthStart.AddMonths(1);
        var daysInMonth = DateTime.DaysInMonth(normalizedMonthStart.Year, normalizedMonthStart.Month);

        var totalsByDay = Enumerable.Range(1, daysInMonth)
            .ToDictionary(
                day => new DateOnly(normalizedMonthStart.Year, normalizedMonthStart.Month, day),
                _ => 0d);

        IReadOnlyList<Guid> simulatorIds;
        if (simulatorId.HasValue)
        {
            simulatorIds = [simulatorId.Value];
        }
        else
        {
            try
            {
                simulatorIds = await GetSimulatorIdsAsync(ct);
            }
            catch
            {
                return new MonthAvailabilityResult(normalizedMonthStart, [], "Unable to load simulators for availability.");
            }
        }

        if (simulatorIds.Count == 0)
        {
            return new MonthAvailabilityResult(normalizedMonthStart, [], "No simulators are available to query.");
        }

        var from = new DateTimeOffset(normalizedMonthStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var to = new DateTimeOffset(monthEndExclusive.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        foreach (var currentSimulatorId in simulatorIds)
        {
            AvailabilityResponse? response;
            try
            {
                response = await http.GetFromJsonAsync<AvailabilityResponse>(
                    $"v1/simulators/{currentSimulatorId}/availability?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}",
                    ct);
            }
            catch
            {
                continue;
            }

            if (response?.AvailableSlots is null)
            {
                continue;
            }

            foreach (var slot in response.AvailableSlots)
            {
                AddSlotHours(totalsByDay, slot.StartAt, slot.EndAt, normalizedMonthStart, monthEndExclusive);
            }
        }

        var days = totalsByDay
            .OrderBy(x => x.Key)
            .Select(x => new DayAvailability(x.Key, Math.Round(x.Value, 2)))
            .ToList();

        return new MonthAvailabilityResult(normalizedMonthStart, days, null);
    }

    private async Task<IReadOnlyList<Guid>> GetSimulatorIdsAsync(CancellationToken ct)
    {
        var page = await http.GetFromJsonAsync<SimulatorPage>("v1/simulators?limit=100", ct);
        return page?.Items?.Select(x => x.UnitId).Distinct().ToList() ?? [];
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var dow = (int)date.DayOfWeek;
        var daysBack = dow == 0 ? 6 : dow - 1;
        return date.AddDays(-daysBack);
    }

    private static void AddSlotHours(
        IDictionary<DateOnly, double> totalsByDay,
        DateTimeOffset start,
        DateTimeOffset end,
        DateOnly monthStart,
        DateOnly monthEndExclusive)
    {
        var clampedStart = DateTimeOffset.Compare(start, new DateTimeOffset(monthStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)) < 0
            ? new DateTimeOffset(monthStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : start;
        var clampedEnd = DateTimeOffset.Compare(end, new DateTimeOffset(monthEndExclusive.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)) > 0
            ? new DateTimeOffset(monthEndExclusive.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : end;

        if (clampedEnd <= clampedStart)
        {
            return;
        }

        var cursor = clampedStart;
        while (cursor < clampedEnd)
        {
            var day = DateOnly.FromDateTime(cursor.UtcDateTime.Date);
            if (!totalsByDay.ContainsKey(day))
            {
                var nextDay = new DateTimeOffset(day.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                cursor = nextDay;
                continue;
            }

            var nextDayBoundary = new DateTimeOffset(day.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var segmentEnd = clampedEnd < nextDayBoundary ? clampedEnd : nextDayBoundary;
            var hours = (segmentEnd - cursor).TotalHours;

            if (hours > 0)
            {
                totalsByDay[day] += hours;
            }

            cursor = segmentEnd;
        }
    }

    private sealed record SimulatorPage(IReadOnlyList<SimulatorItem> Items);
    private sealed record SimulatorItem(Guid UnitId);
    private sealed record AvailabilityResponse(
        IReadOnlyList<AvailableSlot>? AvailableSlots,
        IReadOnlyList<ReconfigurationWindow>? ReconfigurationWindows);
    private sealed record AvailableSlot(DateTimeOffset StartAt, DateTimeOffset EndAt);
    private sealed record ReconfigurationWindow(DateTimeOffset StartAt, DateTimeOffset EndAt, int DurationMins);
}

public sealed record DayAvailability(DateOnly Date, double AvailableHours);
public sealed record MonthAvailabilityResult(DateOnly MonthStart, IReadOnlyList<DayAvailability> Days, string? Warning);
public sealed record WeekReconfigurationWindow(DateTimeOffset StartAt, DateTimeOffset EndAt, int DurationMins, Guid SimulatorId);
public sealed record WeekReconfigurationResult(DateOnly WeekStart, IReadOnlyList<WeekReconfigurationWindow> Windows, string? Warning);

