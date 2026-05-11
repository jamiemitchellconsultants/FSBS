namespace FSBS.Application.Common.Exceptions;

public sealed class AircraftTypeNotFoundException(Guid aircraftTypeId)
    : Exception($"Aircraft type '{aircraftTypeId}' was not found.");

