using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// Describes a specific hardware/software setup of a <see cref="SimulatorUnit"/>
/// for a particular aircraft type and cabin arrangement. Configurations drive
/// pricing, capacity limits, and reconfiguration scheduling.
/// </summary>
/// <remarks>
/// <para>
/// <b>Capacity hard caps</b> (enforced by both domain validators and PostgreSQL
/// CHECK constraints):
/// <list type="bullet">
///   <item>Flight Deck sessions: max 4 students</item>
///   <item>Cabin Crew sessions: max 10 students</item>
/// </list>
/// </para>
/// <para>
/// <b>Training types:</b> <see cref="SupportedTrainingTypes"/> is a PostgreSQL
/// <c>training_type[]</c> array column. A configuration may support one or both
/// training types; bookings are only valid for types present in this list.
/// </para>
/// </remarks>
public class SimulatorConfiguration : AuditableEntity, ISoftDeletable
{
    /// <summary>The simulator unit this configuration belongs to.</summary>
    public Guid SimulatorUnitId { get; set; }
    /// <summary>Human-readable name for this configuration (e.g. "B737-800 Full Flight").</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// ICAO or common designation of the aircraft type this configuration
    /// simulates (e.g. "B737-800", "A320").
    /// </summary>
    public string AircraftType { get; set; } = string.Empty;

    /// <summary>
    /// Physical layout of the simulated cabin. <c>CockpitOnly</c> means the
    /// simulator reproduces only the flight deck; <c>CockpitAndCabin</c> adds
    /// a full cabin mock-up for combined flight-deck and cabin-crew training.
    /// </summary>
    public ConfigurationMode ConfigMode { get; set; }

    /// <summary>
    /// Training types this configuration can host. Stored as a native PostgreSQL
    /// <c>training_type[]</c> array. An instructor's
    /// <see cref="Instructor.TrainingTypeRatings"/> must intersect this list
    /// before the instructor can be assigned to a booking on this configuration.
    /// </summary>
    public List<TrainingType> SupportedTrainingTypes { get; set; } = [];

    /// <summary>
    /// Maximum number of Flight Deck students per booking. Hard cap: 4.
    /// Enforced by <c>ck_simulator_config_fd_capacity</c> CHECK constraint.
    /// </summary>
    public int MaxCapacityFlightDeck { get; set; }

    /// <summary>
    /// Maximum number of Cabin Crew students per booking. Hard cap: 10.
    /// Enforced by <c>ck_simulator_config_cc_capacity</c> CHECK constraint.
    /// </summary>
    public int MaxCapacityCabinCrew { get; set; }

    /// <summary>Whether this configuration is currently available for scheduling.</summary>
    public bool IsActive { get; set; } = true;
    /// <inheritdoc/>
    public bool IsDeleted { get; set; }
    /// <summary>Navigation to the owning simulator unit.</summary>
    public SimulatorUnit SimulatorUnit { get; set; } = null!;

    /// <summary>Schedule templates that use this configuration.</summary>
    public ICollection<ScheduleTemplate> ScheduleTemplates { get; set; } = [];

    /// <summary>Pricing policies that apply to bookings on this configuration.</summary>
    public ICollection<PricingPolicy> PricingPolicies { get; set; } = [];
}
