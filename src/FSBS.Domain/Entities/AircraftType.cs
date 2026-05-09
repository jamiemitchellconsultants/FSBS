namespace FSBS.Domain.Entities;

/// <summary>
/// Reference data entity representing a specific aircraft type that can be
/// simulated (e.g. "B737-800", "A320neo"). Used as a FK on
/// <see cref="SimulatorConfiguration"/> to replace the free-text AircraftType string.
/// </summary>
public class AircraftType : AuditableEntity, ISoftDeletable
{
    /// <summary>ICAO or common designation (e.g. "B737-800"). Unique across non-deleted rows.</summary>
    public string IcaoCode { get; set; } = string.Empty;

    /// <summary>Human-readable display name (e.g. "Boeing 737-800").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether this aircraft type is available for selection on new configurations.</summary>
    public bool IsActive { get; set; } = true;

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Configurations that simulate this aircraft type.</summary>
    public ICollection<SimulatorConfiguration> Configurations { get; set; } = [];
}
