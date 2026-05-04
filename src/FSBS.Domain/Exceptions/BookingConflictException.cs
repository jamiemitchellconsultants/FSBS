namespace FSBS.Domain.Exceptions;

/// <summary>
/// Thrown when a requested slot overlaps an existing booking or reconfiguration
/// window on the same bay.
/// </summary>
public sealed class BookingConflictException(Guid bayId, DateTimeOffset start, DateTimeOffset end)
    : DomainException($"Bay {bayId} is already reserved from {start:O} to {end:O}.");
