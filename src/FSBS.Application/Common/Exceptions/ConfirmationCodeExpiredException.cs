namespace FSBS.Application.Common.Exceptions;

/// <summary>
/// Raised when the confirmation code submitted to Cognito has passed its TTL
/// (24 hours by default). Maps to HTTP 400 Bad Request. The client should
/// direct the user to request a new code via the resend endpoint.
/// </summary>
public sealed class ConfirmationCodeExpiredException()
    : Exception("The confirmation code has expired. Please request a new code.");
