namespace FSBS.Application.Common.Exceptions;

public sealed class AircraftTypeAlreadyExistsException(string icaoCode)
    : Exception($"Aircraft type '{icaoCode}' already exists.");

