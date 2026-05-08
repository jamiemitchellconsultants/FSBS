namespace FSBS.Domain.Entities;

/// <summary>
/// A non-billable buffer period reserved in a <see cref="SimulatorBay"/>
/// to allow technicians to reconfigure the simulator between two bookings
/// that require different <see cref="SimulatorConfiguration"/>s.
/// </summary>
/// <remarks>
/// <para>
/// Reconfiguration slots are created automatically when a booking is confirmed,
/// if the next booking on the same bay requires a different configuration.
/// A slot is also inserted even when there is no subsequent booking, to protect
/// operational readiness for the next session.
/// </para>
/// <para>
/// Users cannot select or book these windows. The availability calendar renders
/// them as grey-hatched blocks. A <c>ReconfigurationAlert</c> notification is
/// dispatched when a reconfig window is less than 60 minutes before the next session.
/// </para>
/// <para>
/// <see cref="DurationMins"/> is sourced from a matching
/// <see cref="ReconfigurationTemplate"/>; when no template exists the fallback
/// is <see cref="SimulatorUnit.DefaultReconfigMins"/>.
/// </para>
/// <para>
/// This entity does not implement <see cref="ISoftDeletable"/> — reconfig slots
/// are hard-deleted when their associated booking is cancelled or rejected and
/// no longer required.
/// </para>
/// </remarks>
public class ReconfigurationSlot : AuditableEntity
{
    /// <summary>The bay in which the reconfiguration will take place.</summary>
    public Guid BayId { get; set; }

    /// <summary>
    /// The <see cref="Booking"/> whose confirmation triggered the creation
    /// of this slot. <c>null</c> when the slot was inserted proactively
    /// with no preceding booking.
    /// </summary>
    public Guid? PrecedingBookingId { get; set; }

    /// <summary>
    /// The <see cref="Booking"/> that will follow this reconfiguration window.
    /// <c>null</c> when the slot was inserted proactively with no subsequent booking.
    /// </summary>
    public Guid? ToBookingId { get; set; }

    /// <summary>UTC start of the reconfiguration window.</summary>
    public DateTimeOffset StartAt { get; set; }

    /// <summary>UTC end of the reconfiguration window.</summary>
    public DateTimeOffset EndAt { get; set; }

    /// <summary>
    /// Duration of the reconfiguration in minutes. Sourced from
    /// <see cref="ReconfigurationTemplate.DurationMins"/> or, if no template
    /// exists, from <see cref="SimulatorUnit.DefaultReconfigMins"/>.
    /// </summary>
    public int DurationMins { get; set; }

    /// <summary>Navigation to the simulator bay.</summary>
    public SimulatorBay Bay { get; set; } = null!;

    /// <summary>Navigation to the booking that preceded this reconfiguration.</summary>
    public Booking? PrecedingBooking { get; set; }

    /// <summary>Navigation to the booking that follows this reconfiguration.</summary>
    public Booking? ToBooking { get; set; }
}
