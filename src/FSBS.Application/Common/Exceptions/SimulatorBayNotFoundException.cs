namespace FSBS.Application.Common.Exceptions;

public sealed class SimulatorBayNotFoundException(Guid simulatorBayId)
    : Exception($"Simulator bay '{simulatorBayId}' was not found.");

