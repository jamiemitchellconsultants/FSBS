namespace FSBS.Application.Common.Exceptions;

public sealed class SimulatorConfigurationNotFoundException(Guid simulatorConfigurationId)
    : Exception($"Simulator configuration '{simulatorConfigurationId}' was not found.");

