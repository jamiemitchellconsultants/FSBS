using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Enums;

namespace FSBS.Application.Bookings.Commands;

/// <summary>
/// Reserves a simulator bay slot and creates a booking in either
/// <c>Provisional</c> (external customers, 15-min hold) or
/// <c>PendingApproval</c> (InternalStudent) state.
/// The <c>Idempotency-Key</c> header value must be passed as
/// <see cref="IdempotencyKey"/> so that client retries are safe.
/// </summary>
public record BookSimulatorSlotCommand(
    Guid BayId,
    Guid ConfigurationId,
    TrainingType TrainingType,
    DateTimeOffset SlotStart,
    DateTimeOffset SlotEnd,
    int StudentCount,
    Guid IdempotencyKey,
    Guid? InstructorId = null,
    string? DepartmentName = null,
    string? BudgetCode = null) : ICommand<BookSimulatorSlotResult>;
