namespace FSBS.Application.Common.Exceptions;

public sealed class SimulatorUnitNotFoundException(Guid simulatorUnitId)
    : Exception($"Simulator unit '{simulatorUnitId}' was not found.");

