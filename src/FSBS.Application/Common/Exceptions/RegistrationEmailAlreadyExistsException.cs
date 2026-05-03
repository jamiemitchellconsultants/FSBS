namespace FSBS.Application.Common.Exceptions;

/// <summary>
/// Raised when a Cognito sign-up is attempted with an email address that is
/// already registered in the customer pool. Maps to HTTP 409 Conflict.
/// </summary>
public sealed class RegistrationEmailAlreadyExistsException(string email)
    : Exception($"An account with email '{email}' already exists.")
{
    public string Email { get; } = email;
}
