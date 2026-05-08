using FluentValidation.TestHelper;
using FSBS.Application.Tests.Common;

namespace FSBS.Application.Tests.Bookings.Validators;

[Trait("Category", "Unit")]
public class BookingCapacityValidatorTests
{
    private static readonly DateTimeOffset Future = DateTimeOffset.UtcNow.AddDays(1);

    private static BookSimulatorSlotCommand Command(
        TrainingType type,
        int students,
        string? department = null,
        string? budget = null) =>
        new(
            BayId: Guid.NewGuid(),
            ConfigurationId: Guid.NewGuid(),
            TrainingType: type,
            SlotStart: Future,
            SlotEnd: Future.AddHours(4),
            StudentCount: students,
            IdempotencyKey: Guid.NewGuid(),
            DepartmentName: department,
            BudgetCode: budget);

    private static BookingCapacityValidator ValidatorFor(AppRole role) =>
        new(new FakeCurrentUser(role));

    // ── StudentCount lower bound ─────────────────────────────────────────────

    [Fact]
    public void StudentCount_Zero_FailsForAnyRole()
    {
        ValidatorFor(AppRole.PrivateCustomer)
            .TestValidate(Command(TrainingType.FlightDeck, 0))
            .ShouldHaveValidationErrorFor(x => x.StudentCount)
            .WithErrorMessage("At least one student is required.");
    }

    // ── FlightDeck capacity (≤ 4) ────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void FlightDeck_AtOrUnderCap_Passes(int students)
    {
        ValidatorFor(AppRole.PrivateCustomer)
            .TestValidate(Command(TrainingType.FlightDeck, students))
            .ShouldNotHaveValidationErrorFor(x => x.StudentCount);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    public void FlightDeck_OverCap_Fails(int students)
    {
        ValidatorFor(AppRole.PrivateCustomer)
            .TestValidate(Command(TrainingType.FlightDeck, students))
            .ShouldHaveValidationErrorFor(x => x.StudentCount)
            .WithErrorMessage("Flight Deck bookings support a maximum of 4 students.");
    }

    // ── CabinCrew capacity (≤ 10) ────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void CabinCrew_AtOrUnderCap_Passes(int students)
    {
        ValidatorFor(AppRole.PrivateCustomer)
            .TestValidate(Command(TrainingType.CabinCrew, students))
            .ShouldNotHaveValidationErrorFor(x => x.StudentCount);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(50)]
    public void CabinCrew_OverCap_Fails(int students)
    {
        ValidatorFor(AppRole.PrivateCustomer)
            .TestValidate(Command(TrainingType.CabinCrew, students))
            .ShouldHaveValidationErrorFor(x => x.StudentCount)
            .WithErrorMessage("Cabin Crew bookings support a maximum of 10 students.");
    }

    // ── InternalStudent required fields ──────────────────────────────────────

    [Fact]
    public void InternalStudent_MissingDepartment_Fails()
    {
        ValidatorFor(AppRole.InternalStudent)
            .TestValidate(Command(TrainingType.FlightDeck, 1, department: null, budget: "B-1"))
            .ShouldHaveValidationErrorFor(x => x.DepartmentName)
            .WithErrorMessage("Department name is required for internal student bookings.");
    }

    [Fact]
    public void InternalStudent_MissingBudgetCode_Fails()
    {
        ValidatorFor(AppRole.InternalStudent)
            .TestValidate(Command(TrainingType.FlightDeck, 1, department: "Flight Ops", budget: null))
            .ShouldHaveValidationErrorFor(x => x.BudgetCode)
            .WithErrorMessage("Budget code is required for internal student bookings.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void InternalStudent_BlankRequiredFields_Fails(string? blank)
    {
        var result = ValidatorFor(AppRole.InternalStudent)
            .TestValidate(Command(TrainingType.FlightDeck, 1, department: blank, budget: blank));

        result.ShouldHaveValidationErrorFor(x => x.DepartmentName);
        result.ShouldHaveValidationErrorFor(x => x.BudgetCode);
    }

    [Fact]
    public void InternalStudent_BothFieldsProvided_Passes()
    {
        var result = ValidatorFor(AppRole.InternalStudent)
            .TestValidate(Command(TrainingType.FlightDeck, 1,
                department: "Flight Ops", budget: "FO-2026-001"));

        result.ShouldNotHaveValidationErrorFor(x => x.DepartmentName);
        result.ShouldNotHaveValidationErrorFor(x => x.BudgetCode);
    }

    // ── Other roles do NOT require the internal-only fields ──────────────────

    [Theory]
    [InlineData(AppRole.PrivateCustomer)]
    [InlineData(AppRole.CorporateManager)]
    [InlineData(AppRole.SalesStaff)]
    public void NonInternalStudent_NullDepartmentAndBudget_Passes(AppRole role)
    {
        var result = ValidatorFor(role)
            .TestValidate(Command(TrainingType.FlightDeck, 2));

        result.ShouldNotHaveValidationErrorFor(x => x.DepartmentName);
        result.ShouldNotHaveValidationErrorFor(x => x.BudgetCode);
    }
}
