namespace FSBS.Web.State.BookingWizard;

public record SetWizardStepAction(int Step);
public record SetWizardBookerRoleAction(string Role);
public record SetWizardOrgAction(Guid? OrgId);
public record SetWizardSimulatorAction(Guid SimulatorId);
public record SetWizardBayAction(Guid BayId);
public record SetWizardConfigurationAction(Guid ConfigurationId);
public record SetWizardDateAction(DateOnly Date);
public record SetWizardSlotAction(TimeOnly Start, TimeOnly End);
public record SetWizardTrainingTypeAction(string TrainingType);
public record SetWizardStudentCountAction(int Count);
public record SetWizardInstructorAction(Guid InstructorId);
public record SetWizardDeptBudgetAction(string DepartmentName, string BudgetCode);
public record SetWizardQuoteAction(decimal PriceGbp);
public record SetWizardSubmittingAction(bool IsSubmitting);
public record SetWizardSubmitErrorAction(string? Error);
public record SetWizardCreatedBookingAction(Guid BookingId);
public record ResetWizardAction;
