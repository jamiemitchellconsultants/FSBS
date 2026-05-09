namespace FSBS.Shared.Simulators;

// ── SimulatorUnit ─────────────────────────────────────────────────────────────

public record CreateSimulatorUnitRequest(
    string Name,
    string FstdLevel,
    string? Manufacturer,
    string? Location,
    int DefaultReconfigMins);

public record UpdateSimulatorUnitRequest(
    string Name,
    string FstdLevel,
    string? Manufacturer,
    string? Location,
    int DefaultReconfigMins,
    bool IsActive);

// ── SimulatorBay ──────────────────────────────────────────────────────────────

public record CreateSimulatorBayRequest(
    string BayCode,
    string? Description);

public record UpdateSimulatorBayRequest(
    string BayCode,
    string? Description,
    string Status);

// ── SimulatorConfiguration ────────────────────────────────────────────────────

public record CreateSimulatorConfigurationRequest(
    string Name,
    Guid AircraftTypeId,
    string ConfigMode,
    IReadOnlyList<string> SupportedTrainingTypes,
    int MaxCapacityFlightDeck,
    int MaxCapacityCabinCrew);

public record UpdateSimulatorConfigurationRequest(
    string Name,
    Guid AircraftTypeId,
    string ConfigMode,
    IReadOnlyList<string> SupportedTrainingTypes,
    int MaxCapacityFlightDeck,
    int MaxCapacityCabinCrew,
    bool IsActive);
