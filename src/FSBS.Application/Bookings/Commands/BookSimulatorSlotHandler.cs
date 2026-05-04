using FluentValidation;
using FluentValidation.Results;
using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Domain.Events;
using FSBS.Domain.Exceptions;
using FSBS.Domain.Interfaces;
using MediatR;

namespace FSBS.Application.Bookings.Commands;

public sealed class BookSimulatorSlotHandler(
    ICurrentUser currentUser,
    IBookingRepository bookingRepository,
    ISimulatorRepository simulatorRepository,
    IReconfigurationSlotRepository reconfigSlotRepository,
    IInstructorRepository instructorRepository)
    : IRequestHandler<BookSimulatorSlotCommand, BookSimulatorSlotResult>
{
    public async Task<BookSimulatorSlotResult> Handle(
        BookSimulatorSlotCommand command, CancellationToken ct)
    {
        // Idempotency: return the existing booking on a client retry.
        var existing = await bookingRepository.FindByIdempotencyKeyAsync(command.IdempotencyKey, ct);
        if (existing is not null)
            return new BookSimulatorSlotResult(existing.Id, existing.Status);

        await ValidateAsync(command, ct);

        var booking = BuildBooking(command);

        await bookingRepository.AddAsync(booking, ct);

        return new BookSimulatorSlotResult(booking.Id, booking.Status);
    }

    private async Task ValidateAsync(BookSimulatorSlotCommand command, CancellationToken ct)
    {
        // DB-side checks that cannot be performed by FluentValidation validators.

        if (await simulatorRepository.FindConfigurationAsync(command.ConfigurationId, ct) is null)
            Fail("ConfigurationId", $"Configuration {command.ConfigurationId} was not found.");

        var conflictingSlots = await bookingRepository.FindConflictingSlotsAsync(
            command.BayId, command.SlotStart, command.SlotEnd, ct);
        if (conflictingSlots.Count > 0)
            throw new BookingConflictException(command.BayId, command.SlotStart, command.SlotEnd);

        if (await reconfigSlotRepository.HasOverlapAsync(command.BayId, command.SlotStart, command.SlotEnd, ct))
            throw new BookingConflictException(command.BayId, command.SlotStart, command.SlotEnd);

        if (command.InstructorId.HasValue)
        {
            var instructor = await instructorRepository.FindByUserIdAsync(command.InstructorId.Value, ct);
            if (instructor is null)
                Fail("InstructorId", $"Instructor {command.InstructorId.Value} was not found.");
            else if (!instructor.TrainingTypeRatings.Contains(command.TrainingType))
                throw new InstructorRatingMismatchException(command.InstructorId.Value, command.TrainingType);
        }
    }

    private Booking BuildBooking(BookSimulatorSlotCommand command)
    {
        var bookingId = Guid.NewGuid();
        var isInternalStudent = currentUser.Role == AppRole.InternalStudent;
        var status = isInternalStudent ? BookingStatus.PendingApproval : BookingStatus.Provisional;

        var slot = new BookingSlot
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            BayId = command.BayId,
            InstructorId = command.InstructorId,
            StartAt = command.SlotStart,
            EndAt = command.SlotEnd,
            DurationMins = (int)(command.SlotEnd - command.SlotStart).TotalMinutes,
            SlotStatus = SlotStatus.Scheduled,
        };

        var booking = new Booking
        {
            Id = bookingId,
            BookedBy = currentUser.UserId,
            OrgId = currentUser.OrgId,
            BookerRole = currentUser.Role,
            TrainingType = command.TrainingType,
            ConfigurationId = command.ConfigurationId,
            StudentCount = command.StudentCount,
            Status = status,
            IdempotencyKey = command.IdempotencyKey,
            DepartmentName = isInternalStudent ? command.DepartmentName : null,
            BudgetCode = isInternalStudent ? command.BudgetCode : null,
            Slots = [slot],
        };

        if (isInternalStudent)
        {
            booking.Approval = new BookingApproval
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                RequestedBy = currentUser.UserId,
                Decision = ApprovalDecision.Pending,
            };
        }

        booking.AddDomainEvent(new SlotBookedEvent(
            BookingId: bookingId,
            BookedBy: currentUser.UserId,
            BookerRole: currentUser.Role,
            TrainingType: command.TrainingType,
            ConfigurationId: command.ConfigurationId,
            StudentCount: command.StudentCount,
            OrgId: currentUser.OrgId,
            SlotStart: command.SlotStart,
            SlotEnd: command.SlotEnd));

        return booking;
    }

    private static void Fail(string propertyName, string message) =>
        throw new ValidationException([new ValidationFailure(propertyName, message)]);
}
