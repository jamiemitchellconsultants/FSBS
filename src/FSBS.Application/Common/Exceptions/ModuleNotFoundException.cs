namespace FSBS.Application.Common.Exceptions;

/// <summary>Thrown when a Module lookup yields no row for the current tenant.</summary>
public sealed class ModuleNotFoundException(Guid moduleId)
    : Exception($"Module '{moduleId}' was not found.");
