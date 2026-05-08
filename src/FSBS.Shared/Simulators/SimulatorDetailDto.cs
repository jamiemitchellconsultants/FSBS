namespace FSBS.Shared.Simulators;

public record SimulatorDetailDto(
    Guid UnitId,
    string Name,
    string FstdLevel,
    bool IsActive,
    IReadOnlyList<SimulatorBayDto> Bays,
    IReadOnlyList<SimulatorConfigurationDto> Configurations);

public record SimulatorBayDto(
    Guid BayId,
    string BayCode,
    string Status);

public record SimulatorConfigurationDto(
    Guid ConfigurationId,
    string Name,
    string AircraftType,
    string ConfigMode,
    IReadOnlyList<string> SupportedTrainingTypes,
    int MaxCapacityFlightDeck,
    int MaxCapacityCabinCrew,
    bool IsActive);
