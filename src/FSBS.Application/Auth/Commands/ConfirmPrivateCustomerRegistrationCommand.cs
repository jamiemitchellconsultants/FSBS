using MediatR;

namespace FSBS.Application.Auth.Commands;

/// <summary>
/// Submits the 6-digit confirmation code sent to the user's email address by
/// Cognito after a successful <see cref="RegisterPrivateCustomerCommand"/>.
/// On success Cognito fires the Post Confirmation Lambda, which creates the
/// <c>AppUser</c> and <c>UserProfile</c> rows in the database.
/// </summary>
public record ConfirmPrivateCustomerRegistrationCommand(
    string Email,
    string ConfirmationCode) : IRequest;
