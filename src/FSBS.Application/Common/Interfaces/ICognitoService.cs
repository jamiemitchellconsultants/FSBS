namespace FSBS.Application.Common.Interfaces;

/// <summary>
/// Abstracts AWS Cognito Identity Provider operations so that handlers in the
/// Application layer remain decoupled from the AWS SDK.
/// </summary>
public interface ICognitoService
{
    /// <summary>
    /// Initiates Cognito sign-up for a private customer. Cognito will send a
    /// confirmation code to <paramref name="email"/>; the user must then call
    /// <see cref="ConfirmSignUpAsync"/> to complete registration.
    /// </summary>
    /// <exception cref="Exceptions.RegistrationEmailAlreadyExistsException">
    /// Thrown when an account with <paramref name="email"/> already exists in the pool.
    /// </exception>
    Task SignUpPrivateCustomerAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string? phoneNumber,
        CancellationToken ct = default);

    /// <summary>
    /// Confirms a Cognito sign-up using the 6-digit code sent to the user's email.
    /// On success Cognito fires the Post Confirmation Lambda, which creates the
    /// <c>AppUser</c> and <c>UserProfile</c> rows in the database.
    /// </summary>
    /// <exception cref="Exceptions.InvalidConfirmationCodeException">
    /// Thrown when the code does not match.
    /// </exception>
    /// <exception cref="Exceptions.ConfirmationCodeExpiredException">
    /// Thrown when the code has expired (default TTL: 24 hours).
    /// </exception>
    Task ConfirmSignUpAsync(
        string email,
        string confirmationCode,
        CancellationToken ct = default);

    /// <summary>
    /// Asks Cognito to re-send the confirmation code to <paramref name="email"/>.
    /// Use this when the original code has expired or was not received.
    /// </summary>
    Task ResendConfirmationCodeAsync(
        string email,
        CancellationToken ct = default);
}
