using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// A specific time block reserved in a <see cref="SimulatorBay"/> for a
/// <see cref="Booking"/>. The bay-time uniqueness constraint prevents
/// double-booking; the duration constraint enforces the minimum 4-hour session rule.
/// </summary>
/// <remarks>
/// <b>Minimum duration:</b> <see cref="DurationMins"/> must be ≥ 240 (4 hours).
/// Enforced by <c>BookingSlotValidator</c> in the application layer and by the
/// <c>ck_booking_slots_min_duration</c> CHECK constraint at the database level.
/// <br/>
/// <b>Double-booking prevention:</b> a filtered unique index on
/// <c>(bay_id, start_at, end_at)</c> where <c>slot_status != 'Cancelled'</c>
/// allows the same time window to be reused after cancellation without a
/// physical delete.
/// </remarks>
public class BookingSlot : AuditableEntity, ISoftDeletable
{
    /// <summary>The booking this slot belongs to.</summary>
    public Guid BookingId { get; set; }

    /// <summary>The simulator bay where the session will take place.</summary>
    public Guid BayId { get; set; }

    /// <summary>
    /// Instructor assigned to deliver this session. <c>null</c> until an
    /// instructor is scheduled. Assignment requires the instructor's
    /// <see cref="Instructor.TrainingTypeRatings"/> to include the parent
    /// booking's <see cref="Booking.TrainingType"/>.
    /// </summary>
    public Guid? InstructorId { get; set; }

    /// <summary>UTC start time of the session.</summary>
    public DateTimeOffset StartAt { get; set; }

    /// <summary>UTC end time of the session.</summary>
    public DateTimeOffset EndAt { get; set; }

    /// <summary>
    /// Duration in minutes. Must be ≥ 240. Stored explicitly rather than
    /// derived from <see cref="StartAt"/>/<see cref="EndAt"/> to simplify
    /// constraint enforcement and reporting queries.
    /// </summary>
    public int DurationMins { get; set; }

    /// <summary>Current status of the slot within the session lifecycle.</summary>
    public SlotStatus SlotStatus { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the parent booking.</summary>
    public Booking Booking { get; set; } = null!;

    /// <summary>Navigation to the reserved simulator bay.</summary>
    public SimulatorBay Bay { get; set; } = null!;

    /// <summary>Navigation to the assigned instructor, if any.</summary>
    public Instructor? Instructor { get; set; }
}
