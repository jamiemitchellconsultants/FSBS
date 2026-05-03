using Fluxor;

namespace FSBS.Web.State.Calendar;

[FeatureState]
public record CalendarState
{
    public Guid? SimulatorId { get; init; }
    public DateOnly WeekStart { get; init; } = DateOnly.FromDateTime(DateTime.Today);
    public bool IsLoading { get; init; }
    public IReadOnlyList<object> AvailabilityGrid { get; init; } = [];
    public IReadOnlyList<object> ReconfigWindows { get; init; } = [];
    public IReadOnlyList<object> MaintenanceWindows { get; init; } = [];
}
