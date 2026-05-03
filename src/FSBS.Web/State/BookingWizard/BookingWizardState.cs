using Fluxor;

namespace FSBS.Web.State.BookingWizard;

[FeatureState]
public record BookingWizardState
{
    public int CurrentStep { get; init; } = 1;
    public Guid? SelectedSimulatorId { get; init; }
    public DateOnly? SelectedDate { get; init; }
    public TimeOnly? SlotStart { get; init; }
    public TimeOnly? SlotEnd { get; init; }
    public string? TrainingType { get; init; }
    public int StudentCount { get; init; } = 1;
    public Guid? InstructorId { get; init; }
    public string? DepartmentName { get; init; }
    public string? BudgetCode { get; init; }
    public decimal? QuotedPriceGbp { get; init; }
}
