namespace FSBS.Application.Common.Exceptions;

/// <summary>
/// Thrown when an authenticated user attempts an operation that their role
/// does not permit. The global exception handler maps this to <c>403 Forbidden</c>.
/// </summary>
public sealed class ForbiddenException(string message) : Exception(message);
