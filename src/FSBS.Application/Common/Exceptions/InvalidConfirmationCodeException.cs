namespace FSBS.Application.Common.Exceptions;

/// <summary>
/// Raised when the confirmation code submitted to Cognito does not match the
/// code that was issued. Maps to HTTP 400 Bad Request.
/// </summary>
public sealed class InvalidConfirmationCodeException()
    : Exception("The confirmation code is incorrect.");
