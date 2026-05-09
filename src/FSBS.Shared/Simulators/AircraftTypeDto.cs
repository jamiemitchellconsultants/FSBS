namespace FSBS.Shared.Simulators;

public record AircraftTypeDto(
    Guid AircraftTypeId,
    string IcaoCode,
    string Name,
    bool IsActive);
