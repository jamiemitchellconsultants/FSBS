using MediatR;

namespace FSBS.Application.Auth.Commands;

/// <summary>
/// Asks Cognito to re-send the confirmation code to the user's email address.
/// Use when the original code has expired (24-hour TTL) or was not received.
/// </summary>
public record ResendConfirmationCodeCommand(string Email) : IRequest;
