using Fluxor;

namespace FSBS.Web.State.BookingWizard;

public static class BookingWizardReducers
{
    [ReducerMethod]
    public static BookingWizardState OnSetStep(BookingWizardState state, SetWizardStepAction a) =>
        state with { CurrentStep = a.Step };

    [ReducerMethod]
    public static BookingWizardState OnSetSimulator(BookingWizardState state, SetWizardSimulatorAction a) =>
        state with { SelectedSimulatorId = a.SimulatorId };

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

    [ReducerMethod(typeof(ResetWizardAction))]
    public static BookingWizardState OnReset(BookingWizardState _) => new();
}
