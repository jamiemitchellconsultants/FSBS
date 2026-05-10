using Fluxor;
using FSBS.Shared.InstructorSchedule;

namespace FSBS.Web.State.InstructorSchedule;

public enum ScheduleViewMode { Week, Month }

[FeatureState]
public record InstructorScheduleState
{
    /// <summary>
    /// The instructor whose schedule is currently being viewed. Null = "me"
    /// (resolved server-side from the JWT). Set explicitly when ScheduleAdmin
    /// is editing on someone's behalf.
    /// </summary>
    public Guid? InstructorId { get; init; }

    public ScheduleViewMode View { get; init; } = ScheduleViewMode.Week;

    /// <summary>
    /// Anchor date driving the visible window. For Week view this is treated
    /// as "any date inside the visible week"; for Month view as "any date
    /// inside the visible month".
    /// </summary>
    public DateOnly AnchorDate { get; init; } = DateOnly.FromDateTime(DateTime.Today);

    public InstructorScheduleDto? Schedule { get; init; }
    public bool IsLoading { get; init; }
    public string? Error { get; init; }
}
