using FluentValidation;
using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Enums;

namespace FSBS.Application.Bookings.Commands;

/// <summary>
/// Enforces the capacity caps defined in CLAUDE.md (FlightDeck ≤ 4,
/// CabinCrew ≤ 10) and the InternalStudent mandatory-field rule
/// (DepartmentName + BudgetCode both required).
/// ICurrentUser is injected so the role-specific rules can be evaluated
/// at request time without embedding role in the command itself.
/// </summary>
public sealed class BookingCapacityValidator : AbstractValidator<BookSimulatorSlotCommand>
{
    public BookingCapacityValidator(ICurrentUser currentUser)
    {
        RuleFor(x => x.StudentCount)
            .GreaterThanOrEqualTo(1)
            .WithMessage("At least one student is required.");

        When(x => x.TrainingType == TrainingType.FlightDeck, () =>
            RuleFor(x => x.StudentCount)
                .LessThanOrEqualTo(4)
                .WithMessage("Flight Deck bookings support a maximum of 4 students."));

        When(x => x.TrainingType == TrainingType.CabinCrew, () =>
            RuleFor(x => x.StudentCount)
                .LessThanOrEqualTo(10)
                .WithMessage("Cabin Crew bookings support a maximum of 10 students."));

        if (currentUser.Role == AppRole.InternalStudent)
        {
            RuleFor(x => x.DepartmentName)
                .NotEmpty()
                .WithMessage("Department name is required for internal student bookings.");

            RuleFor(x => x.BudgetCode)
                .NotEmpty()
                .WithMessage("Budget code is required for internal student bookings.");
        }
    }
}
