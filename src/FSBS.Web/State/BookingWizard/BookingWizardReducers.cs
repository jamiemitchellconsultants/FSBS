using Fluxor;

namespace FSBS.Web.State.BookingWizard;

public static class BookingWizardReducers
{
    [ReducerMethod]
    public static BookingWizardState OnSetStep(BookingWizardState state, SetWizardStepAction a) =>
        state with { CurrentStep = a.Step };

    [ReducerMethod]
    public static BookingWizardState OnSetBookerRole(BookingWizardState state, SetWizardBookerRoleAction a) =>
        state with { BookerRole = a.Role };

    [ReducerMethod]
    public static BookingWizardState OnSetOrg(BookingWizardState state, SetWizardOrgAction a) =>
        state with { OrgId = a.OrgId };

    [ReducerMethod]
    public static BookingWizardState OnSetSimulator(BookingWizardState state, SetWizardSimulatorAction a) =>
        state with { SelectedSimulatorId = a.SimulatorId, SelectedBayId = null, SelectedConfigurationId = null };

    [ReducerMethod]
    public static BookingWizardState OnSetBay(BookingWizardState state, SetWizardBayAction a) =>
        state with { SelectedBayId = a.BayId };

    [ReducerMethod]
    public static BookingWizardState OnSetConfiguration(BookingWizardState state, SetWizardConfigurationAction a) =>
        state with { SelectedConfigurationId = a.ConfigurationId };

    [ReducerMethod]
    public static BookingWizardState OnSetDate(BookingWizardState state, SetWizardDateAction a) =>
        state with { SelectedDate = a.Date };

    [ReducerMethod]
    public static BookingWizardState OnSetSlot(BookingWizardState state, SetWizardSlotAction a) =>
        state with { SlotStart = a.Start, SlotEnd = a.End };

    [ReducerMethod]
    public static BookingWizardState OnSetTrainingType(BookingWizardState state, SetWizardTrainingTypeAction a) =>
        state with { TrainingType = a.TrainingType };

    [ReducerMethod]
    public static BookingWizardState OnSetStudentCount(BookingWizardState state, SetWizardStudentCountAction a) =>
        state with { StudentCount = a.Count };

    [ReducerMethod]
    public static BookingWizardState OnSetInstructor(BookingWizardState state, SetWizardInstructorAction a) =>
        state with { InstructorId = a.InstructorId };

    [ReducerMethod]
    public static BookingWizardState OnSetDeptBudget(BookingWizardState state, SetWizardDeptBudgetAction a) =>
        state with { DepartmentName = a.DepartmentName, BudgetCode = a.BudgetCode };

    [ReducerMethod]
    public static BookingWizardState OnSetQuote(BookingWizardState state, SetWizardQuoteAction a) =>
        state with { QuotedPriceGbp = a.PriceGbp };

    [ReducerMethod]
    public static BookingWizardState OnSetSubmitting(BookingWizardState state, SetWizardSubmittingAction a) =>
        state with { IsSubmitting = a.IsSubmitting };

    [ReducerMethod]
    public static BookingWizardState OnSetSubmitError(BookingWizardState state, SetWizardSubmitErrorAction a) =>
        state with { SubmitError = a.Error };

    [ReducerMethod]
    public static BookingWizardState OnSetCreatedBooking(BookingWizardState state, SetWizardCreatedBookingAction a) =>
        state with { CreatedBookingId = a.BookingId };

    [ReducerMethod(typeof(ResetWizardAction))]
    public static BookingWizardState OnReset(BookingWizardState _) => new();
}
