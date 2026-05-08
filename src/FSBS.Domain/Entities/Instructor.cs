using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// A staff member authorised to conduct simulator sessions. An instructor's
/// eligibility for a booking depends on their <see cref="TrainingTypeRatings"/>
/// intersecting the booking's training type.
/// </summary>
/// <remarks>
/// Before assigning an instructor to a <see cref="BookingSlot"/>, the handler
/// must verify that the instructor's <see cref="TrainingTypeRatings"/> list
/// contains the booking's <see cref="Booking.TrainingType"/>. This check is also
/// enforced in the <c>GET /instructors?trainingType=</c> query so the UI only
/// presents eligible instructors to the scheduler.
/// </remarks>
public class Instructor : AuditableEntity, ISoftDeletable
{
    /// <summary>
    /// The <see cref="AppUser"/> account of this instructor. Must hold the
    /// <c>AppRole.Instructor</c> role.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>Unique employee number assigned by HR (e.g. "EMP-0042").</summary>
    public string EmployeeNumber { get; set; } = string.Empty;

    /// <summary>Maximum number of hours per week this instructor may be scheduled.</summary>
    public short MaxHoursPerWeek { get; set; }

    /// <summary>The date on which the instructor was hired.</summary>
    public DateOnly HireDate { get; set; }

    /// <summary>
    /// Training types this instructor is rated to deliver. Stored as a native
    /// PostgreSQL <c>training_type[]</c> array. An instructor may hold ratings
    /// for Flight Deck, Cabin Crew, or both. Assigning an instructor to a
    /// session whose type is absent from this list is a domain error.
    /// </summary>
    public List<TrainingType> TrainingTypeRatings { get; set; } = [];

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the user account.</summary>
    public AppUser User { get; set; } = null!;

    /// <summary>
    /// Declared availability and leave windows used by the scheduler to
    /// identify eligible instructors for a given slot.
    /// </summary>
    public ICollection<InstructorAvailability> Availabilities { get; set; } = [];

    /// <summary>Booking slots to which this instructor has been assigned.</summary>
    public ICollection<BookingSlot> BookingSlots { get; set; } = [];
}
