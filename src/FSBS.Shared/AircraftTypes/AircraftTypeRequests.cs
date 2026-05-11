namespace FSBS.Shared.AircraftTypes;

/// <summary>Request body for creating a new aircraft type.</summary>
public record CreateAircraftTypeRequest(string IcaoCode, string Name);

/// <summary>Request body for updating an existing aircraft type.</summary>
public record UpdateAircraftTypeRequest(string IcaoCode, string Name, bool IsActive);

