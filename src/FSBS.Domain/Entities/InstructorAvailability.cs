using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// A declared time window indicating whether an <see cref="Instructor"/> is
/// available to deliver sessions, is on leave, or has another commitment.
/// Used by the scheduling service to filter eligible instructors when a
/// <see cref="BookingSlot"/> is being assigned.
/// </summary>
public class InstructorAvailability : AuditableEntity, ISoftDeletable
{
    /// <summary>The instructor this window applies to.</summary>
    public Guid InstructorId { get; set; }

    /// <summary>UTC start of the availability window.</summary>
    public DateTimeOffset StartAt { get; set; }

    /// <summary>UTC end of the availability window.</summary>
    public DateTimeOffset EndAt { get; set; }

    /// <summary>
    /// Indicates whether this window represents available time
    /// (<see cref="AvailabilityType.Available"/>), approved leave
    /// (<see cref="AvailabilityType.Leave"/>), or another commitment
    /// (<see cref="AvailabilityType.Other"/>). Only <c>Available</c> windows
    /// make the instructor eligible for session assignment.
    /// </summary>
    public AvailabilityType AvailabilityType { get; set; }

    /// <summary>
    /// Optional free-text note (e.g. leave type, external commitment description).
    /// </summary>
    public string? Notes { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the instructor this window belongs to.</summary>
    public Instructor Instructor { get; set; } = null!;
}
