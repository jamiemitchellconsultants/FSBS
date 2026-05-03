using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// A physical bay within a <see cref="SimulatorUnit"/> where training sessions
/// take place. Each <see cref="BookingSlot"/> and <see cref="ReconfigurationSlot"/>
/// targets a specific bay, and the unique index on <c>(bay_id, start_at, end_at)</c>
/// prevents double-booking.
/// </summary>
public class SimulatorBay : AuditableEntity, ISoftDeletable
{
    /// <summary>The simulator unit this bay belongs to.</summary>
    public Guid SimulatorUnitId { get; set; }

    /// <summary>
    /// Short display name for the bay as shown on the scheduling calendar
    /// (e.g. "Bay 1", "West Wing Bay").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Operational state of the bay. <c>Operational</c> bays are bookable;
    /// <c>Maintenance</c> and <c>Decommissioned</c> bays are blocked from
    /// new bookings and displayed as dark grey on the availability calendar.
    /// </summary>
    public BayStatus Status { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the owning simulator unit.</summary>
    public SimulatorUnit SimulatorUnit { get; set; } = null!;

    /// <summary>All booking slots reserved in this bay.</summary>
    public ICollection<BookingSlot> BookingSlots { get; set; } = [];

    /// <summary>
    /// Non-billable reconfiguration buffer slots in this bay. Rendered as
    /// grey-hatched on the availability calendar and cannot be selected by users.
    /// </summary>
    public ICollection<ReconfigurationSlot> ReconfigurationSlots { get; set; } = [];
}
