using MediatR;

namespace FSBS.Application.Auth.Commands;

/// <summary>
/// Initiates Cognito sign-up for a new private customer. Cognito sends a
/// 6-digit confirmation code to <see cref="Email"/>; the client must follow
/// up with <see cref="ConfirmPrivateCustomerRegistrationCommand"/>.
/// </summary>
/// <remarks>
/// No invitation token is required for private customers — they can self-register
/// freely. The <c>custom:registration_type</c> attribute is set to <c>private</c>
/// in the Cognito SignUp request so the Pre Sign-up Lambda can distinguish this
/// path from corporate invitation-based registrations.
/// </remarks>
public record RegisterPrivateCustomerCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber) : IRequest;
