using FSBS.Domain.Entities;
using FSBS.Shared.Simulators;

namespace FSBS.Application.Simulators;

internal static class SimulatorDtoMapper
{
    public static SimulatorDetailDto ToDetail(SimulatorUnit unit) =>
        new(
            UnitId: unit.Id,
            Name: unit.Name,
            FstdLevel: unit.FstdLevel,
            Manufacturer: unit.Manufacturer,
            Location: unit.Location,
            DefaultReconfigMins: unit.DefaultReconfigMins,
            IsActive: unit.IsActive,
            Bays: unit.Bays
                .Where(b => !b.IsDeleted)
                .Select(ToBay)
                .ToList(),
            Configurations: unit.Configurations
                .Where(c => !c.IsDeleted)
                .Select(ToConfiguration)
                .ToList());

    public static SimulatorBayDto ToBay(SimulatorBay bay) =>
        new(bay.Id, bay.BayCode, bay.Status.ToString());

    public static SimulatorConfigurationDto ToConfiguration(SimulatorConfiguration configuration) =>
        new(configuration.Id, configuration.Name,
            configuration.AircraftTypeId,
            configuration.AircraftType?.IcaoCode ?? string.Empty,
            configuration.AircraftType?.Name ?? string.Empty,
            configuration.ConfigMode.ToString(),
            configuration.SupportedTrainingTypes.Select(t => t.ToString()).ToList(),
            configuration.MaxCapacityFlightDeck, configuration.MaxCapacityCabinCrew, configuration.IsActive);
}

