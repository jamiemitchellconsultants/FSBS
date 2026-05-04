using FSBS.Domain.Enums;

namespace FSBS.Domain.Exceptions;

/// <summary>
/// Thrown when the requested student count exceeds the hard capacity cap for
/// the booking's training type (4 for FlightDeck, 10 for CabinCrew).
/// </summary>
public sealed class BookingCapacityExceededException(
    TrainingType trainingType,
    int requested,
    int maximum)
    : DomainException(
        $"{trainingType} bookings support a maximum of {maximum} students; {requested} were requested.");
