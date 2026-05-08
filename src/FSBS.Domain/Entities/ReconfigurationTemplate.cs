namespace FSBS.Domain.Entities;

/// <summary>
/// Defines the turnaround duration required to switch a <see cref="SimulatorUnit"/>
/// from one <see cref="SimulatorConfiguration"/> to another. One template exists
/// per ordered config pair; the reverse direction requires its own template.
/// </summary>
/// <remarks>
/// A unique constraint on <c>(from_config_id, to_config_id)</c> and a CHECK
/// constraint ensuring <c>from_config_id != to_config_id</c> enforce integrity
/// at the database level. When no matching template exists for a transition the
/// system falls back to <see cref="SimulatorUnit.DefaultReconfigMins"/>.
/// The resulting time block is written as a <see cref="ReconfigurationSlot"/>
/// when a booking is confirmed.
/// </remarks>
public class ReconfigurationTemplate : AuditableEntity
{
    /// <summary>The configuration the unit is switching <em>away from</em>.</summary>
    public Guid FromConfigId { get; set; }

    /// <summary>The configuration the unit is switching <em>to</em>.</summary>
    public Guid ToConfigId { get; set; }

    /// <summary>
    /// How many minutes the reconfiguration takes. This is the duration of
    /// the non-billable <see cref="ReconfigurationSlot"/> inserted between
    /// the two adjacent bookings.
    /// </summary>
    public int DurationMins { get; set; }

    /// <summary>The configuration being transitioned from.</summary>
    public SimulatorConfiguration FromConfiguration { get; set; } = null!;

    /// <summary>The configuration being transitioned to.</summary>
    public SimulatorConfiguration ToConfiguration { get; set; } = null!;
}
